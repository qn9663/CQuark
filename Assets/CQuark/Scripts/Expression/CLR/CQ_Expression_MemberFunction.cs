﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CQuark {

    public class CQ_Expression_MemberFunction : ICQ_Expression {
        public CQ_Expression_MemberFunction (int tbegin, int tend, int lbegin, int lend) {
            _expressions = new List<ICQ_Expression>();
            this.tokenBegin = tbegin;
            this.tokenEnd = tend;
            lineBegin = lbegin;
            lineEnd = lend;
        }
        public int lineBegin {
            get;
            private set;
        }
        public int lineEnd {
            get;
            private set;
        }
        //Block的参数 一个就是一行，顺序执行，没有
        public List<ICQ_Expression> _expressions {
            get;
            private set;
        }
        public int tokenBegin {
            get;
            private set;
        }
        public int tokenEnd {
            get;
            private set;
        }
        public bool hasCoroutine {
            get {
                if(_expressions == null || _expressions.Count == 0)
                    return false;
                foreach(ICQ_Expression expr in _expressions) {
                    if(expr.hasCoroutine)
                        return true;
                }
                return false;
            }
        }
        MethodCache cache = null;

        public CQ_Value ComputeValue (CQ_Content content) {
#if CQUARK_DEBUG
            content.InStack(this);
#endif
            CQ_Value parent = _expressions[0].ComputeValue(content);

#if CQUARK_DEBUG
            if(parent == CQ_Value.Null) {
                throw new Exception("调用空对象的方法:" + _expressions[0].ToString() + ":" + ToString());
            }
#endif


            CQ_Value[] parameters = CQ_ObjPool.PopArray(_expressions.Count - 1);
            for(int i = 0; i < _expressions.Count - 1; i++) {
                parameters[i] = _expressions[i + 1].ComputeValue(content);
            }

            CQ_Value value = CQ_Value.Null;

            //这几行是为了快速获取Unity的静态变量，而不需要反射
            object obj = parent.GetObject();
            if(!Wrap.MemberCall(parent.m_type, obj, functionName, parameters, out value)) {
                //TODO 要么注册所有基本类型（bool,int,string...)要么这里特殊处理
                if(functionName == "ToString" && parameters.Length == 0) {
                    CQ_Value ret = new CQ_Value();
                    ret.SetObject(typeof(string), obj.ToString());
                    return ret;
				}else if(obj is UnityEngine.MonoBehaviour && functionName == "StartCoroutine" &&
					parameters.Length >= 1 && parameters[0].GetObject() is CQ_Expression_Block){
					//从西瓜调用的ClassSystem.StartCoroutine(CquarkMethod());不需要走cache
					UnityEngine.MonoBehaviour mb = obj as UnityEngine.MonoBehaviour;
					CQ_Expression_Block call = parameters[0].GetObject()  as CQ_Expression_Block;

					CQ_Value ret = new CQ_Value();
					ret.SetObject(typeof(UnityEngine.Coroutine),
						mb.StartCoroutine(call.callObj.CallType.CoroutineCall(call, call.callObj, mb)));
					return ret;
				}
                else {
                    var iclass = CQuark.AppDomain.GetITypeByCQValue(parent)._class;
                    if(cache == null || cache.cachefail) {
                        cache = new MethodCache();
                        value = iclass.MemberCall(content, obj, functionName, parameters, cache);
                    }
                    else {
                        value = iclass.MemberCallCache(content, obj, parameters, cache);
                    }
                }
            }


#if CQUARK_DEBUG
            content.OutStack(this);
#endif
            CQ_ObjPool.PushArray(parameters);
            return value;
        }
        public IEnumerator CoroutineCompute (CQ_Content content, UnityEngine.MonoBehaviour coroutine) {
            throw new Exception("暂时不支持套用协程");
        }

        public string functionName;

        public override string ToString () {
            return "MemberCall|a." + functionName;
        }
    }
}