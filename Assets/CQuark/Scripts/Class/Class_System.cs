﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CQuark {
    public class Class_System : IClass {
        public Type type {
            get;
            private set;
        }
        Dictionary<int, System.Reflection.MethodInfo> cacheT;//= new Dictionary<string, System.Reflection.MethodInfo>();

        public Class_System (Type type) {
            this.type = type;
        }
        public CQ_Value New (CQ_Content content, CQ_Value[] _params) {
            Type[] types = new Type[_params.Length];
            object[] objparams = new object[_params.Length];
            for(int i = 0; i < _params.Length; i++) {
                types[i] = _params[i].m_type;
                objparams[i] = _params[i].GetObject();
            }
            CQ_Value value = new CQ_Value();
            ConstructorInfo con = this.type.GetConstructor(types);
            if(con == null) {
                value.SetObject(type, Activator.CreateInstance(this.type));
            }
            else {
                value.SetObject(type, con.Invoke(objparams));
            }
            return value;
        }


        public CQ_Value StaticCall (CQ_Content content, string function, CQ_Value[] _params) {
            return StaticCall(content, function, _params, null);
        }
        public CQ_Value StaticCall (CQ_Content content, string function, CQ_Value[] _params, MethodCache cache) {
            //TODO 这里应该优先匹配类型完全相同（含默认参），没有的话再匹配隐式
            bool needConvert = false;
            List<object> _oparams = new List<object>();
            List<Type> types = new List<Type>();
            bool pIsEmpty = false;
            foreach(CQ_Value p in _params) {
                _oparams.Add(p.GetObject());
                if(p.m_stype != null) {
                    types.Add(typeof(object));
                }
                else {
                    if(p.m_type == null) {
                        pIsEmpty = true;
                    }
                    types.Add(p.m_type);
                }
            }
            System.Reflection.MethodInfo methodInfo = null;
            if(!pIsEmpty)
                methodInfo = type.GetMethod(function, types.ToArray());
            //if (methodInfo == null && type.BaseType != null)//加上父类型静态函数查找,典型的现象是 GameObject.Destory
            //{
            //    methodInfo = type.BaseType.GetMethod(function, types.ToArray());
            //}
            if(methodInfo == null) {
                if(function[function.Length - 1] == '>')//这是一个临时的模板函数调用
				{
                    int sppos = function.IndexOf('<', 0);
                    string tfunc = function.Substring(0, sppos);
                    string strparam = function.Substring(sppos + 1, function.Length - sppos - 2);
                    string[] sf = strparam.Split(',');
                    //string tfunc = sf[0];
                    Type[] gtypes = new Type[sf.Length];
                    for(int i = 0; i < sf.Length; i++) {
                        gtypes[i] = CQuark.AppDomain.GetTypeByKeyword(sf[i]).typeBridge;
                    }
                    methodInfo = FindMethod(type, tfunc, _params, gtypes);

                }
                if(methodInfo == null) {
                    Type ptype = type.BaseType;
                    while(ptype != null) {
                        methodInfo = ptype.GetMethod(function, types.ToArray());
                        if(methodInfo != null) break;
                        var t = CQuark.AppDomain.GetITypeByType(ptype);
                        try {
                            return t._class.StaticCall(content, function, _params, cache);
                        }
                        catch {

                        }
                        ptype = ptype.BaseType;
                    }

                }
            }
            if(methodInfo == null) {
                methodInfo = GetMethodSlow(content, true, function, types, _oparams);
                needConvert = true;
            }

            if(methodInfo == null) {
                throw new Exception("函数不存在function:" + type.ToString() + "." + function);
            }
            if(cache != null) {
                cache.info = methodInfo;
                cache.needConvert = needConvert;
            }

            CQ_Value v = new CQ_Value();
            v.SetObject(methodInfo.ReturnType, methodInfo.Invoke(null, _oparams.ToArray()));
            
            return v;
        }

        public CQ_Value StaticCallCache (CQ_Content content, CQ_Value[] _params, MethodCache cache) {
            List<object> _oparams = new List<object>();

            foreach(var p in _params) {
                _oparams.Add(p.GetObject());
            }
            System.Reflection.MethodInfo methodInfo = cache.info;
            if(cache.needConvert) {
                var pp = methodInfo.GetParameters();
                for(int i = 0; i < pp.Length; i++) {
                    if(i >= _params.Length) {
                        _oparams.Add(pp[i].DefaultValue);
                    }
                    else {
                        if(pp[i].ParameterType != _params[i].m_type) {
                            _oparams[i] = _params[i].ConvertTo(pp[i].ParameterType);
                        }
                    }
                }
            }
            CQ_Value v = new CQ_Value();
            v.SetObject(methodInfo.ReturnType, methodInfo.Invoke(null, _oparams.ToArray()));
            
            return v;
        }

        public CQ_Value StaticValueGet (CQ_Content content, string valuename) {
            CQ_Value v = MemberValueGet(content, null, valuename);
            if(v == CQ_Value.Null) {
                if(type.BaseType != null) {
					return CQuark.AppDomain.GetITypeByType(type.BaseType)._class.StaticValueGet(content, valuename);
                }
                else {
                    throw new NotImplementedException();
                }
            }
            else {
                return v;
            }
        }

        public bool StaticValueSet (CQ_Content content, string valuename, CQ_Value value) {
            bool b = MemberValueSet(content, null, valuename, value);
            if(!b) {
                if(type.BaseType != null) {
					CQuark.AppDomain.GetITypeByType(type.BaseType)._class.StaticValueSet(content, valuename, value);
                    return true;
                }
                else {
                    throw new NotImplementedException();
                }
            }
            else {
                return b;
            }
        }


        public CQ_Value MemberCall (CQ_Content content, object object_this, string function, CQ_Value[] _params) {
            return MemberCall(content, object_this, function, _params, null);
        }
        public CQ_Value MemberCall (CQ_Content content, object object_this, string function, CQ_Value[] _params, MethodCache cache) {
            //TODO 这里应该优先匹配类型完全相同（含默认参），没有的话再匹配隐式
            bool needConvert = false;
            List<Type> types = new List<Type>();
            List<object> _oparams = new List<object>();
            bool pIsEmpty = false;
            foreach(CQ_Value p in _params) {
                _oparams.Add(p.GetObject());
                
                if(p.m_stype != null) {
                    types.Add(typeof(object));
                }
                else {
                    if(p.m_type == null) {
                        pIsEmpty = true;
                    }
                    types.Add(p.m_type);
                }
            }

            System.Reflection.MethodInfo methodInfo = null;
            if(!pIsEmpty) {
                methodInfo = type.GetMethod(function, types.ToArray());
            }
            CQ_Value v = new CQ_Value();
            if(methodInfo == null) {
                if(function[function.Length - 1] == '>')//这是一个临时的模板函数调用
				{
                    int sppos = function.IndexOf('<', 0);
                    string tfunc = function.Substring(0, sppos);
                    string strparam = function.Substring(sppos + 1, function.Length - sppos - 2);
                    string[] sf = strparam.Split(',');
                    //string tfunc = sf[0];
                    Type[] gtypes = new Type[sf.Length];
                    for(int i = 0; i < sf.Length; i++) {
                        gtypes[i] = CQuark.AppDomain.GetTypeByKeyword(sf[i]).typeBridge;
                    }
                    methodInfo = FindMethod(type, tfunc, _params, gtypes);
                    var ps = methodInfo.GetParameters();
                    for(int i = 0; i < Math.Min(ps.Length, _oparams.Count); i++) {
                        if(ps[i].ParameterType != _params[i].m_type) {

                            _oparams[i] = _params[i].ConvertTo(ps[i].ParameterType);
                        }
                    }
                }
                else {
                    if(!pIsEmpty) {
                        foreach(var s in type.GetInterfaces()) {
                            methodInfo = s.GetMethod(function, types.ToArray());
                            if(methodInfo != null) break;
                        }
                    }
                    if(methodInfo == null) {//因为有cache的存在，可以更慢更多的查找啦，哈哈哈哈
                        methodInfo = GetMethodSlow(content, false, function, types, _oparams);
                        needConvert = true;
                    }
                    if(methodInfo == null) {
                        throw new Exception("函数不存在function:" + type.ToString() + "." + function);
                    }
                }
            }
            if(cache != null) {
                cache.info = methodInfo;
                cache.needConvert = needConvert;
            }

            if(methodInfo == null) {
                throw new Exception("函数不存在function:" + type.ToString() + "." + function);
            }
            v.SetObject(methodInfo.ReturnType, methodInfo.Invoke(object_this, _oparams.ToArray()));
            
            return v;
        }

        System.Reflection.MethodInfo FindMethod (Type type, string func, CQ_Value[] _params, Type[] gtypes) {
            string hashkey = func + "_" + _params.Length + ":";
            foreach(var p in _params) {
                hashkey += p.m_type.ToString();
            }
            foreach(var t in gtypes) {
                hashkey += t.ToString();
            }
            int hashcode = hashkey.GetHashCode();
            MethodInfo methodInfo = null;
            if(cacheT == null) {
                cacheT = new Dictionary<int, System.Reflection.MethodInfo>();
            }
            else if(cacheT.TryGetValue(hashcode, out methodInfo)) {
                return methodInfo;
            }
            //+"~" + (sf.Length - 1).ToString();
            var ms = type.GetMethods();
            foreach(var t in ms) {
                if(t.Name == func && t.IsGenericMethodDefinition) {
                    var pp = t.GetParameters();
                    if(pp.Length != _params.Length)
                        continue;
                    bool match = true;
                    for(int i = 0; i < pp.Length; i++) {
                        if(pp[i].ParameterType.IsGenericType) continue;
                        if(pp[i].ParameterType.IsGenericParameter) continue;
                        if(pp[i].ParameterType != _params[i].m_type) {
                            match = false;
                            break;
                        }
                    }
                    if(match) {
                        methodInfo = t.MakeGenericMethod(gtypes);
                        cacheT[hashcode] = methodInfo;
                        return methodInfo;
                    }
                }
            }
            return null;
        }

        public IEnumerator CoroutineCall (CQ_Content content, object object_this, string function, CQ_Value[] _params, UnityEngine.MonoBehaviour coroutine) {
            //TODO 不存在這樣的調用
            MemberCall(content, object_this, function, _params, null);
            yield return null;
        }

        Dictionary<string, IList<System.Reflection.MethodInfo>> slowCache = null;

        System.Reflection.MethodInfo GetMethodSlow (CQuark.CQ_Content content, bool bStatic, string funcname, IList<Type> types, IList<object> _params) {
            List<object> myparams = new List<object>(_params);
            if(slowCache == null) {
                System.Reflection.MethodInfo[] ms = this.type.GetMethods();
                slowCache = new Dictionary<string, IList<System.Reflection.MethodInfo>>();
                foreach(var m in ms) {
                    string name = m.IsStatic ? "s=" + m.Name : m.Name;
                    if(slowCache.ContainsKey(name) == false) {
                        slowCache[name] = new List<System.Reflection.MethodInfo>();
                    }
                    slowCache[name].Add(m);
                }
            }
            IList<System.Reflection.MethodInfo> minfo = null;

            if(slowCache.TryGetValue(bStatic ? "s=" + funcname : funcname, out minfo) == false)
                return null;

            foreach(var m in minfo) {
                bool match = true;
                var pp = m.GetParameters();
                if(pp.Length < types.Count)//参数多出来，不匹配
				{
                    match = false;
                    continue;
                }
                for(int i = 0; i < pp.Length; i++) {
                    if(i >= types.Count)//参数多出来
					{
                        if(!pp[i].IsOptional) {

                            match = false;
                            break;
                        }
                        else {
                            myparams.Add(pp[i].DefaultValue);
                            continue;
                        }
                    }
                    else {
                        if(pp[i].ParameterType == types[i])
                            continue;

                        try {
                            if(types[i] == null && !pp[i].ParameterType.IsValueType) {
                                continue;
                            }
							myparams[i] = CQuark.AppDomain.GetITypeByType(types[i]).ConvertTo(_params[i], pp[i].ParameterType);
                            if(myparams[i] == null) {
                                match = false;
                                break;
                            }
                        }
                        catch {
                            match = false;
                            break;
                        }
                    }
                    if(match)
                        break;
                }
                if(!match) {
                    continue;
                }
                else {
                    for(int i = 0; i < myparams.Count; i++) {
                        if(i < _params.Count) {
                            _params[i] = myparams[i];
                        }
                        else {
                            _params.Add(myparams[i]);
                        }
                    }
                    return m;
                }

            }

            if(minfo.Count == 1)
                return minfo[0];

            return null;

        }
		//TODO 函数调用优先级需要重写。优先类型全等，再是隐式转换
        public CQ_Value MemberCallCache (CQ_Content content, object object_this, CQ_Value[] _params, MethodCache cache) {
            System.Reflection.MethodInfo methodInfo = cache.info;
            CQ_Value v = new CQ_Value();
            if(cache.needConvert) {
                List<object> _oparams = new List<object>();
                foreach(var p in _params) {
                    _oparams.Add(p.GetObject());
                }
                var pp = methodInfo.GetParameters();
                for(int i = 0; i < pp.Length; i++) {
                    if(i >= _params.Length) {
                        _oparams.Add(pp[i].DefaultValue);
                    }
                    else {
                        if(pp[i].ParameterType != _params[i].m_type) {
                            _oparams[i] = _params[i].ConvertTo(pp[i].ParameterType);
                        }
                    }
                }
                v.SetObject(methodInfo.ReturnType, methodInfo.Invoke(object_this, _oparams.ToArray()));
                
            }
            else {
                object[] _oparams = new object[_params.Length];
                for(int i = 0; i < _params.Length; i++) {
                    _oparams[i] = _params[i].GetObject();
                }
                v.SetObject(methodInfo.ReturnType, methodInfo.Invoke(object_this, _oparams));
               
            }

            return v;
        }

        class MemberValueCache {
            public int type = 0;
            public System.Reflection.FieldInfo finfo;
            public System.Reflection.MethodInfo minfo;
            public System.Reflection.EventInfo einfo;
        }


        Dictionary<string, MemberValueCache> memberValueGetCaches = new Dictionary<string, MemberValueCache>();
        public CQ_Value MemberValueGet (CQ_Content content, object object_this, string valuename) {
            MemberValueCache c = null;

            if(!memberValueGetCaches.TryGetValue(valuename, out c)) {
                c = new MemberValueCache();
                memberValueGetCaches[valuename] = c;
                c.finfo = type.GetField(valuename);
                if(c.finfo == null) {
                    c.minfo = type.GetMethod("get_" + valuename);
                    if(c.minfo == null) {
                        c.einfo = type.GetEvent(valuename);
                        if(c.einfo == null) {
                            c.type = -1;
                            return CQ_Value.Null;
                        }
                        else {
                            c.type = 3;
                        }
                    }
                    else {
                        c.type = 2;
                    }
                }
                else {
                    c.type = 1;
                }
            }

            if(c.type < 0)
                return CQ_Value.Null;
            CQ_Value v = new CQ_Value();
            switch(c.type) {
                case 1:
                    v.SetObject(c.finfo.FieldType, c.finfo.GetValue(object_this));
                    break;
                case 2:
                    v.SetObject(c.minfo.ReturnType, c.minfo.Invoke(object_this, null));
                    break;
                case 3:
                    v.SetObject(c.einfo.EventHandlerType, new DeleEvent(object_this, c.einfo));
                    break;
            }
            return v;
        }


        Dictionary<string, MemberValueCache> memberValueSetCaches = new Dictionary<string, MemberValueCache>();
        public bool MemberValueSet (CQ_Content content, object object_this, string valuename, CQ_Value value) {
            MemberValueCache c;

            if(!memberValueSetCaches.TryGetValue(valuename, out c)) {
                c = new MemberValueCache();
                memberValueSetCaches[valuename] = c;
                c.finfo = type.GetField(valuename);
                if(c.finfo == null) {
                    c.minfo = type.GetMethod("set_" + valuename);
                    //                    var mss = type.GetMethods();
                    if(c.minfo == null) {
                        if(type.GetMethod("add_" + valuename) != null) {
                            c.type = 3;//event;
                        }
                        else {
                            c.type = -1;
                            return false;
                        }
                    }

                    else {
                        c.type = 2;
                    }
                }
                else {
                    c.type = 1;
                }
            }

            if(c.type < 0)
                return false;

            object obj = value.GetObject();

            if(c.type == 1) {
                if(obj != null && obj.GetType() != c.finfo.FieldType) {
                    obj = CQuark.AppDomain.ConvertTo(obj, c.finfo.FieldType);
                }
                c.finfo.SetValue(object_this, obj);
            }
            else if(c.type == 2) {
                var ptype = c.minfo.GetParameters()[0].ParameterType;
                if(obj != null && obj.GetType() != ptype) {

                    obj = CQuark.AppDomain.ConvertTo(obj, ptype);
                }
                c.minfo.Invoke(object_this, new object[] { obj });
            }
            return true;
        }



        System.Reflection.MethodInfo indexGetCache = null;
        Type indexGetCachetypeindex;
        Type indexGetCacheType = null;
        public CQ_Value IndexGet (CQ_Content content, object object_this, object key) {
            //var m = type.GetMembers();
            if(indexGetCache == null) {
                indexGetCache = type.GetMethod("get_Item");
                if(indexGetCache != null) {
                    indexGetCacheType = indexGetCache.ReturnType;
                }
                if(indexGetCache == null) {
                    indexGetCache = type.GetMethod("GetValue", new Type[] { typeof(int) });
                    if(indexGetCache != null) {
                        indexGetCacheType = type.GetElementType();
                    }

                }
                indexGetCachetypeindex = indexGetCache.GetParameters()[0].ParameterType;
            }
            //else
            {
                CQ_Value v = new CQ_Value();
                if(key != null && key.GetType() != indexGetCachetypeindex)
                    key = CQuark.AppDomain.ConvertTo(key, (CQuark.TypeBridge)indexGetCachetypeindex);
                v.SetObject(indexGetCacheType, indexGetCache.Invoke(object_this, new object[] { key }));
                return v;
            }
            //throw new NotImplementedException();

        }


        System.Reflection.MethodInfo indexSetCache = null;
        Type indexSetCachetype1;
        Type indexSetCachetype2;
        bool indexSetCachekeyfirst = false;
        public void IndexSet (CQ_Content content, object object_this, object key, object value) {
            if(indexSetCache == null) {
                indexSetCache = type.GetMethod("set_Item");
                indexSetCachekeyfirst = true;
                if(indexSetCache == null) {
                    indexSetCache = type.GetMethod("SetValue", new Type[] { typeof(object), typeof(int) });
                    indexSetCachekeyfirst = false;
                }
                var pp = indexSetCache.GetParameters();
                indexSetCachetype1 = pp[0].ParameterType;
                indexSetCachetype2 = pp[1].ParameterType;
            }
            //else
            if(indexSetCachekeyfirst) {

                if(key != null && key.GetType() != indexSetCachetype1)
                    key = CQuark.AppDomain.ConvertTo(key, (CQuark.TypeBridge)indexSetCachetype1);
                if(value != null && value.GetType() != indexSetCachetype2)
                    value = CQuark.AppDomain.ConvertTo(value, (CQuark.TypeBridge)indexSetCachetype2);
                indexSetCache.Invoke(object_this, new object[] { key, value });
            }
            else {
                if(value != null && value.GetType() != indexSetCachetype1)
                    value = CQuark.AppDomain.ConvertTo(value, (CQuark.TypeBridge)indexSetCachetype1);
                if(key != null && key.GetType() != indexSetCachetype2)
                    key = CQuark.AppDomain.ConvertTo(key, (CQuark.TypeBridge)indexSetCachetype2);

                indexSetCache.Invoke(object_this, new object[] { value, key });
            }
        }
    }
}
