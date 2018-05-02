﻿using System.Collections.Generic;
using System;
using System.Reflection;

public class Property {
    public string m_type;
    public bool m_isStatic;
    public bool m_canGet;
    public bool m_canSet;
    public string m_name;
    public bool m_obsolete;

    public Property (Type type, bool isStatic, bool canGet, bool canSet, string name, bool obsolete) {
        m_type = WrapTextTools.Type2String(type);
        m_isStatic = isStatic;
        m_canGet = canGet;
        m_canSet = canSet;
        m_name = name;
        m_obsolete = obsolete;
    }
}

public class Method {
    /// <summary>
    /// "New, SCall, MCall, Op..."
    /// </summary>
    public string m_methodType;
    public string m_returnType;
    public string m_methodName;
    public string[] m_inType;
    public bool m_isGeneric = false; //是否是<T>
    public bool m_obsolete;

    public Method (string methodType, Type returnType, string methodName, System.Reflection.ParameterInfo[] param, int paramLength, bool isGeneric, bool obsolete) {
        m_methodType = methodType;
        if(returnType != null)
            m_returnType = WrapTextTools.Type2String(returnType); ;
        m_methodName = methodName;
        m_inType = new string[paramLength];

        for(int i = 0; i < paramLength; i++) {
            m_inType[i] = WrapTextTools.Type2String(param[i].ParameterType);
        }
        m_isGeneric = isGeneric;
        m_obsolete = obsolete;
    }
}

public class WrapReflectionTools {

    public static Type GetType (string fullName) {
        switch(fullName) {
            case "double":
                return typeof(double);
            case "float":
                return typeof(float);
            case "long":
                return typeof(long);
            case "ulong":
                return typeof(ulong);
            case "int":
                return typeof(int);
            case "uint":
                return typeof(uint);
            case "short":
                return typeof(short);
            case "ushort":
                return typeof(ushort);
            case "byte":
                return typeof(byte);
            case "sbyte":
                return typeof(sbyte);
            case "char":
                return typeof(char);

            case "string":
                return typeof(string);
            case "bool":
                return typeof(bool);

            case "Type":
                return typeof(Type);
            case "object":
                return typeof(object);
        }
        //TODO 这里改为黑名单
        if(fullName.StartsWith("CQuark")) {
            DebugUtil.LogError("不允许对CQuark做Wrap");
            return null;
        }

        Type type = Type.GetType(fullName); //如System.DateTime
        if(type != null) {
            return type;
        }

        AssemblyName[] referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

        string fullNameWithPlus = fullName.Replace('.', '+');
        foreach(var assemblyName in referencedAssemblies) {
            // Load the referenced assembly
            var assembly = Assembly.Load(assemblyName);
            if(assembly != null) {
                type = assembly.GetType(fullNameWithPlus);
                if(type != null)    //如DebugUtil
                    return type;
            }
        }

        string[] split = fullName.Split('.');

        for(int i = 0; i < split.Length - 1; i++){
            string nameSpace = split[0];
            for(int j = 0; j < i; j++){
                nameSpace += "." + split[j + 1];
            }
            string typeName = split[i+1];
            for(int j = i + 1; j < split.Length - 1; j++){
                typeName += "+" + split[j+1];
            }

            try {
                Assembly assembly = Assembly.Load(nameSpace);
                if(assembly != null) {
                    type = assembly.GetType(nameSpace + "." + typeName);
                    if(type != null) {
                        //如UnityEngine.Vector3
                        return type;
                    }
                    type = assembly.GetType(typeName);
                    if(type != null)
                        return type;
                }
            }
            catch(Exception) {
                foreach(var assemblyName in referencedAssemblies) {
                    Assembly assembly = Assembly.Load(assemblyName);
                    if(assembly != null) {
                        //如LitJson.JSONNode
                        type = assembly.GetType(nameSpace + "." + typeName);
                        if(type != null)
                            return type;
                    }
                }
            }
        }
        return null;
    }

    public static string GetTrueName (Type type) {
        return type.FullName.Replace('+','.');
    }

    public static string GetWrapName (Type type) {
        return type.FullName.Replace("+", "").Replace(".", "");
    }

    public static string GetWrapFolderName (Type type) {
        if(string.IsNullOrEmpty(type.Namespace))
            return "";
        return type.Namespace;
    }

    public static string GetWrapFileName (Type type) {
        string s = type.FullName;
        if(!string.IsNullOrEmpty(type.Namespace))
            s = s.Substring(GetWrapFolderName(type).Length + 1);
        return s.Replace('+', '.');
    }


	public static Type GetType( string TypeName, ref string nameSpace){
		if(string.IsNullOrEmpty(nameSpace)){
			switch(TypeName){
			case "double":
				return typeof(double);
			case "float":
				return typeof(float);
			case "long":
				return typeof(long);
			case "ulong":
				return typeof(ulong);
			case "int":
				return typeof(int);
			case "uint":
				return typeof(uint);
			case "short":
				return typeof(short);
			case "ushort":
				return typeof(ushort);
			case "byte":
				return typeof(byte);
			case "sbyte":
				return typeof(sbyte);
			case "char":
				return typeof(char);

			case "string":
				return typeof(string);
			case "bool":
				return typeof(bool);
//							List<>
//                          Dictionary<,>
//                          Stack<>
//                          Queue<>
			}
		}
		Type type = null;
		if(string.IsNullOrEmpty(nameSpace)){
			type = Type.GetType( TypeName );
			if(type != null){
				return type;
			}else{
				AssemblyName[] referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
				foreach( var assemblyName in referencedAssemblies ){
					// Load the referenced assembly
					var assembly = Assembly.Load( assemblyName );
					if( assembly != null ){
						type = assembly.GetType(TypeName );
						//如DebugUtil
						if( type != null )
							return type;
						
						nameSpace = "";
						type = assembly.GetType( assemblyName.ToString() + "." + TypeName );
						if( type != null )
							return type;
					}
				}
			}
		}else{
			//TODO 这里改为黑名单
			if(nameSpace == "CQuark"){
				DebugUtil.LogError("不允许对CQuark做Wrap");
				return null;
			}
			type = Type.GetType( nameSpace + "." + TypeName  );
			if(type != null){
				//如System.DateTime
				return type;
			}
			try{
				Assembly assembly = Assembly.Load( nameSpace );
				if( assembly != null ){
					type = assembly.GetType( nameSpace + "." + TypeName );
					if( type != null ){
						//如UnityEngine.Vector3
						return type;
					}
					type = assembly.GetType(TypeName );
					if( type != null )
						return type;
				}
			}catch (Exception){
				AssemblyName[] referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
				foreach( var assemblyName in referencedAssemblies ){
					Assembly assembly = Assembly.Load( assemblyName );
					if( assembly != null ){
						//如LitJson.JSONNode
						type = assembly.GetType( nameSpace + "." + TypeName );
						if( type != null )
							return type;
					}
				}
			}
		}
		return null;
	}

	//给出一个命名空间，返回所有Type。nameSpace可以为空
	public static Type[] GetTypesByNamespace(string nameSpace){
		Assembly assembly = null;
        List<Type> types = new List<Type>();
		try{
			assembly = Assembly.Load( nameSpace );
			if( assembly != null ){
				Type[] typeArray = assembly.GetTypes();
                foreach(var t in typeArray) {
                    if(!IsTClass(t))
                        types.Add(t);
                }
			}
		}catch (Exception){
			AssemblyName[] referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

			foreach( var assemblyName in referencedAssemblies ){
                 if(string.IsNullOrEmpty(nameSpace) && !assemblyName.FullName.StartsWith("Assembly-CSharp"))
                     continue;

				assembly = Assembly.Load( assemblyName );
				if(assembly != null ){
                   
					Type[] typeArray = assembly.GetTypes();
					foreach(var t in typeArray){
						if(string.IsNullOrEmpty(nameSpace) && string.IsNullOrEmpty(t.Namespace)){
                            if(!IsTClass(t))
                                types.Add(t);
                            else
                                DebugUtil.LogWarning("Obsolete " + t.Name);
						}
						else if(t.Namespace == nameSpace){
                            if(!IsTClass(t))
    							types.Add(t);
						}
					}
				}
			}
		}
        return types.ToArray();
	}

    public static List<Property> GetPropertys (Type type, ref string manifest) {
		List<Property> savePropertys = new List<Property>();
		manifest += "\n字段\n";
		FieldInfo[] fis = type.GetFields();
		for(int i= 0; i < fis.Length; i++){
			bool isObsolete = IsObsolete(fis[i]);
			if(isObsolete)
				manifest += "[Obsolete]";
			
			manifest += "public ";
			string attributes = fis[i].Attributes.ToString();
			bool isStatic = attributes.Contains("Static");
			if(isStatic)
				manifest += "static ";
			bool isReadonly = attributes.Contains("Literal") && attributes.Contains("HasDefault") ; //const
			isReadonly = isReadonly || attributes.Contains("InitOnly");                             //readonly
			if(isReadonly)
				manifest += "readonly ";
            manifest += WrapTextTools.Type2String(fis[i].FieldType) + " " + fis[i].Name + ";\n";
			savePropertys.Add(new Property(fis[i].FieldType, isStatic, true, !isReadonly, fis[i].Name, isObsolete));
		}
		
		manifest += "\n静态属性\n";
		PropertyInfo[] spis = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
		for(int i= 0; i < spis.Length; i++){
			bool isObsolete = IsObsolete(spis[i]);
			if(isObsolete)
				manifest += "[Obsolete]";
			
			manifest += "public static ";
//			string attributes = spis[i].Attributes.ToString();
            manifest += WrapTextTools.Type2String(spis[i].PropertyType) + " " + spis[i].Name + "{";
			if(spis[i].CanRead) 
				manifest += "get;";
			if(spis[i].CanWrite)
				manifest += "set;";
			manifest += "}\n";

			savePropertys.Add(new Property(spis[i].PropertyType, true, spis[i].CanRead, spis[i].CanWrite, spis[i].Name, isObsolete));
		}

		manifest += "\n实例属性\n";
		PropertyInfo[] pis = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		//检查是否有Index
		MethodInfo[] mis = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
		bool hasIndex = false;
		for(int j = 0; j < mis.Length; j++){
			if(mis[j].Name == "get_Item" || mis[j].Name == "set_Item"){
				hasIndex = true;
				break;
			}
		}
		
		for(int i = 0; i < pis.Length; i++){
			bool isObsolete = IsObsolete(pis[i]);

			if(pis[i].Name == "Item" && hasIndex){
				continue;//如果有get_Item或者set_Item，表示是[Index]，否则表示一个属性
			}

			if(isObsolete)
				manifest += "[Obsolete]";
			manifest += "public ";
//			string attributes = pis[i].Attributes.ToString();
            manifest += WrapTextTools.Type2String(pis[i].PropertyType) + " " + pis[i].Name + "{";
			if(pis[i].CanRead) 
				manifest += "get;";
			if(pis[i].CanWrite)
				manifest += "set;";
			manifest += "}\n";

				
			savePropertys.Add(new Property(pis[i].PropertyType, false, pis[i].CanRead, pis[i].CanWrite, pis[i].Name, isObsolete));
		}

		return savePropertys;
	}

    public static List<Method> GetConstructor (Type type, ref string manifest) {
		List<Method> saveMethods = new List<Method>();
		manifest += "\n构造函数\n";
		if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type)) {
			manifest += "继承自MonoBehaviour\n";
			return saveMethods;
		}
		System.Reflection.ConstructorInfo[] construct = type.GetConstructors();
		for(int i = 0; i < construct.Length; i++){
			bool isObsolete = IsObsolete(construct[i]);

			string s = "";
			if(isObsolete)
				manifest += "[Obsolete]";
			s += "public ";
			s += type.ToString() + "(";
			System.Reflection.ParameterInfo[] param = construct[i].GetParameters();
			if(param.Length == 0){
				saveMethods.Add(new Method("New", null, "", param, 0, false, isObsolete));
			}else{
				if(param[0].IsOptional)
					saveMethods.Add(new Method("New", null, "", param, 0, false, isObsolete));
				
				for(int j = 0; j < param.Length; j++){
					s += param[j].ParameterType + " " +param[j].Name;
					if(j != param.Length - 1)
						s += ",";
					if(param[j].IsOptional)
						s += " = " + param[j].DefaultValue;
					
					if(j == param.Length - 1 || param[j + 1].IsOptional) {
						saveMethods.Add(new Method("New", null, "", param, j + 1, false, isObsolete));
					}
				}
			}
			s += ")";
			manifest += s + "\n";
		}

		return saveMethods;
	}

    public static List<Method> GetStaticMethods (Type type, ref string manifest, List<Property> ignoreProperty) {
		List<Method> saveMethods = new List<Method> ();
		//静态函数，成员函数
		manifest += "\n静态方法\n";

		//TODO 暂时不包含op，不包含index，不包含ref，in, out, 不包含IEnumrator
		System.Reflection.MethodInfo[] smis = type.GetMethods (BindingFlags.Public | BindingFlags.Static);//这里没有获取私有方法，因为即使获取了西瓜也没有办法调用
		//基类的方法一起导出，这样可以自动调用基类
		for (int i = 0; i < smis.Length; i++) {
			bool isObsolete = IsObsolete(smis[i]);

			//属性上面已经获取过了 这里做个判断，是否包含这个属性，包含的话continue，否则表示有方法名是get_,set_开头
			if (ContainsProperty (ignoreProperty, smis [i].Name))
				continue;

			if (IsOp (smis [i]))
				continue;

            string retType = WrapTextTools.Type2String(smis[i].ReturnType);

			string s = "";
			if(isObsolete)
				s += "[Obsolete]";
			s += "public static " + retType + " " ;
			s += smis [i].Name ;
			bool isGeneric = (smis[i].ContainsGenericParameters && smis[i].IsGenericMethod);
			if(isGeneric)
				s += "<T>";
            s += "(";
			System.Reflection.ParameterInfo[] param = smis [i].GetParameters ();
			if (param.Length == 0) {
				saveMethods.Add (new Method ("SCall", smis [i].ReturnType, smis [i].Name, param, 0, isGeneric, isObsolete));
			} else {
				if (param [0].IsOptional)
					saveMethods.Add (new Method ("SCall", smis [i].ReturnType, smis [i].Name, param, 0, isGeneric, isObsolete));
				for (int j = 0; j < param.Length; j++) {
                    string paramType = WrapTextTools.Type2String(param[j].ParameterType);
					bool finish = true;
					if (paramType.EndsWith ("&")) {//ref，out
						s += "ref " + paramType.Substring (0, paramType.Length - 1) + " " + param [j].Name;
						finish = false;
					} else {
						s += paramType + " " + param [j].Name;
						if (param [j].IsOptional)
							s += " = " + param [j].DefaultValue;
					}
					if (j != param.Length - 1)
						s += ", ";
					
					if (finish) {
						if (j == param.Length - 1 || param [j + 1].IsOptional) {
							saveMethods.Add (new Method ("SCall", smis [i].ReturnType, smis [i].Name, param, j + 1, isGeneric, isObsolete));
						}
					}
				}
			}
			s += ")";
			manifest += s + "\n";
		}
		return saveMethods;
	}

	public static List<Method> GetInstanceMethods(Type type, ref string manifest, List<Property> ignoreProperty){
		List<Method> saveMethods = new List<Method> ();
		manifest += "\n成员方法\n";

		if (type.GetConstructors ().Length == 0 && !typeof(UnityEngine.Component).IsAssignableFrom(type)) {	//是否是静态类，静态类没有构造函数
			manifest += "静态类没有成员方法";
			return saveMethods;			//静态类依然会反射出成员方法（比如ToString,GetType），但没法调用，我们不保存
		}

		System.Reflection.MethodInfo[] imis = type.GetMethods (BindingFlags.Public | BindingFlags.Instance);//这里没有获取私有方法，因为即使获取了西瓜也没有办法调用
		//基类的方法一起导出，这样可以自动调用基类

		PropertyInfo[] pis = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		bool hasIndex = false;
		for (int i = 0; i < pis.Length; i++) {
			if(pis[i].Name == "Item"){
				hasIndex = true;
				break;
			}
		}
		for (int i = 0; i < imis.Length; i++) {
			bool isObsolete = IsObsolete(imis[i]);

			//属性上面已经获取过了 这里做个判断，是否包含这个属性，包含的话continue，否则表示有方法名是get_,set_开头
			if(ContainsProperty(ignoreProperty, imis[i].Name))
				continue;

			if(imis[i].Name == "get_Item" || imis[i].Name == "set_Item"){
				if(hasIndex)
					continue;
			}

            string retType = WrapTextTools.Type2String(imis[i].ReturnType);

			string s = "";
			if(isObsolete)
				s += "[Obsolete]";
			s += "public " + retType + " ";
			s += imis [i].Name ;
			bool isGeneric = (imis[i].ContainsGenericParameters && imis[i].IsGenericMethod);
			if(isGeneric)
				s += "<T>";

			s += "(";
			System.Reflection.ParameterInfo[] param = imis [i].GetParameters ();
			if (param.Length == 0) {
				saveMethods.Add (new Method ("MCall", imis [i].ReturnType, imis [i].Name, param, 0, isGeneric, isObsolete));
			} else {
				if (param [0].IsOptional)
					saveMethods.Add (new Method ("MCall", imis [i].ReturnType, imis [i].Name, param, 0, isGeneric, isObsolete));
				for (int j = 0; j < param.Length; j++) {
                    string paramType = WrapTextTools.Type2String(param[j].ParameterType);
					bool finish = true;
					if (paramType.EndsWith ("&")) {//ref，out
						s += "ref " + paramType.Substring (0, paramType.Length - 1) + " " + param [j].Name;
						finish = false;
					} else {
						s += paramType + " " + param [j].Name;
						if (param [j].IsOptional)
							s += " = " + param [j].DefaultValue;
					}
					if (j != param.Length - 1)
						s += ", ";
					
					if (finish) {
						if (j == param.Length - 1 || param [j + 1].IsOptional) {
							saveMethods.Add (new Method ("MCall", imis [i].ReturnType, imis [i].Name, param, j + 1, isGeneric, isObsolete));
						}
					}
				}
			}
			s += ")";
			manifest += s + "\n";
		}
		return saveMethods;
	}

    public static List<Method> GetIndex (Type type, ref string manifest) {
		if (type.GetConstructors ().Length == 0)	//是否是静态类，静态类没有构造函数
			return new List<Method> ();			//静态类依然会反射出成员方法（比如ToString,GetType），但没法调用，我们不保存
		
		List<Method> saveMethods = new List<Method> ();
		manifest += "\n索引\n";

		PropertyInfo[] pis = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		bool hasIndex = false;
		for (int i = 0; i < pis.Length; i++) {
			if(pis[i].Name == "Item"){
				hasIndex = true;
				break;
			}
		}
		if (!hasIndex)
			return new List<Method> ();

		//检查是否有Index
		MethodInfo[] mis = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
		for (int i = 0; i < mis.Length; i++) {
			if(mis[i].Name == "get_Item"){
				bool isObsolete = IsObsolete(mis[i]);

				ParameterInfo[] pais = mis[i].GetParameters();
				Method method = new Method("IndexGet", mis[i].ReturnType, mis[i].Name, pais, pais.Length, false, isObsolete);
				saveMethods.Add(method);
				if(isObsolete)
					manifest += "[Obsolete]";
				manifest += "IndexGet " + mis[i].ReturnType + " [";
				for(int j = 0; j < pais.Length; j++){
					manifest += pais[j].ParameterType + " " + pais[j].Name;
					if(j != pais.Length - 1)
						manifest += ",";
				}
				manifest += "]\n";
			}else if(mis[i].Name == "set_Item"){
				bool isObsolete = IsObsolete(mis[i]);

				ParameterInfo[] pais = mis[i].GetParameters();
				Method method = new Method("IndexSet", mis[i].ReturnType, mis[i].Name, mis[i].GetParameters(), mis[i].GetParameters().Length, false, isObsolete);
				saveMethods.Add(method);
				if(isObsolete)
					manifest += "[Obsolete]";
				manifest += "IndexSet [";
				for(int j = 0; j < pais.Length - 1; j++){
					manifest += pais[j].ParameterType + " " + pais[j].Name;
					if(j != pais.Length - 2)
						manifest += ",";
				}
				manifest += "] = ";
				manifest += pais[pais.Length - 1].ParameterType + " " + pais[pais.Length - 1].Name + "\n";
			}
		}
		return saveMethods;
	}

    public static List<Method> GetOp (Type type, ref string manifest) {
		List<Method> saveMethods = new List<Method> ();

		manifest += "\n运算符\n";

		System.Reflection.MethodInfo[] ops = type.GetMethods (BindingFlags.Public | BindingFlags.Static);//和获取静态方法一样
		for (int i = 0; i < ops.Length; i++) {
			bool isObsolete = IsObsolete(ops[i]);

			if (!IsOp (ops [i]))
				continue;

			System.Reflection.ParameterInfo[] param = ops [i].GetParameters ();
			if(isObsolete)
				manifest += "[Obsolete]";
			manifest += "public static " + ops[i].ReturnType + " " + ops[i].Name + "(";
            for(int j = 0; j < param.Length; j++) {
                manifest += param[j].ParameterType + " " + param[j].Name;
                if(j != param.Length - 1)
                    manifest += ",";
            }
            manifest += ")\n";

			Method method = new Method(ops[i].Name, ops[i].ReturnType, ops[i].Name, param, param.Length, false, isObsolete);
			saveMethods.Add(method);
		}
		return saveMethods;
	}

    static bool ContainsProperty (List<Property> propertys, string methodName) {
        if(methodName.StartsWith("get_")) {
            string propertyName = methodName.Substring(4);
            for(int i = 0; i < propertys.Count; i++) {
                if(propertys[i].m_canGet && propertys[i].m_name == propertyName)
                    return true;
            }
        }
        else if(methodName.StartsWith("set_")) {
            string propertyName = methodName.Substring(4);
            for(int i = 0; i < propertys.Count; i++) {
                if(propertys[i].m_canSet && propertys[i].m_name == propertyName)
                    return true;
            }
        }
        return false;
    }

	static bool IsOp(MethodInfo method){
        //if (method.GetParameters ().Length != 2)
        //    return false;
		string methodName = method.Name;
        int paramLength = method.GetParameters().Length;
        if(paramLength == 2) {
             return 
                 methodName == "op_Addition"            //a+b
            || methodName == "op_Subtraction"           //a-b
            || methodName == "op_Multiply"              //a*b
            || methodName == "op_Division"              //a/b
            || methodName == "op_Modulus"               //a%b

            || methodName == "op_GreaterThan"           //a>b
            || methodName == "op_LessThan"              //a<b
            || methodName == "op_GreaterThanOrEqual"    //a>=b
            || methodName == "op_LessThanOrEqual"       //a<=b

            || methodName == "op_Equality"              //a==b
            || methodName == "op_Inequality";           //a!=b
        }
        else if(paramLength == 1){
            return
                methodName == "op_Implicit"            //a 隐式转换成T
            || methodName == "op_Explicit"             //(T)a

            || methodName == "op_UnaryNegation" ;      //-a
        }
        return false;            
	}


	static bool IsObsolete(MemberInfo mb){
		object[] attrs = mb.GetCustomAttributes(true);
		for (int j = 0; j < attrs.Length; j++){
			Type t = attrs[j].GetType() ;            

			if (t == typeof(System.ObsoleteAttribute) || t.Name == "MonoNotSupportedAttribute" || t.Name == "MonoTODOAttribute"){
				return true;               
			}
		}
		return false;
	}    

    
    static bool IsTClass (Type type) {
        if(type.FullName.Contains("<"))//TODO 模版类判断不应该在这里
            return true;
        return false;
        //Type t = type.Attributes.GetType();
        //if(t == typeof(System.ObsoleteAttribute) || t.Name == "MonoNotSupportedAttribute" || t.Name == "MonoTODOAttribute") {
        //    return true;
        //}
        //return false;
    }
}
