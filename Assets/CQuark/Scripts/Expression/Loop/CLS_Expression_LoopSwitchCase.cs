﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CSLE
{

    public class CLS_Expression_LoopSwitchCase : ICLS_Expression
    {
		public CLS_Expression_LoopSwitchCase(int tbegin, int tend, int lbegin, int lend)
        {
            listParam = new List<ICLS_Expression>();
            tokenBegin = tbegin;
            tokenEnd = tend;

            lineBegin = lbegin;
            lineEnd = lend;
        }
        public int lineBegin
        {
            get;
            private set;
        }
        public int lineEnd
        {
            get;
            set;
        }
        
        public List<ICLS_Expression> listParam
        {
            get;
            private set;
        }
        public int tokenBegin
        {
            get;
            private set;
        }
        public int tokenEnd
        {
            get;
            set;
        }
		public bool hasCoroutine{
			get{
				if(listParam == null || listParam.Count == 0)
					return false;
				foreach(ICLS_Expression expr in listParam){
					if(expr.hasCoroutine)
						return true;
				}
				return false;
			}
		}
        public CLS_Content.Value ComputeValue(CLS_Content content)
        {
            content.InStack(this);
            content.DepthAdd();
            ICLS_Expression expr_switch = listParam[0] as ICLS_Expression;
			CLS_Content.Value switchVal = null;
//			CLS_Content.Value vrt = null;
			if (expr_switch != null) 
				switchVal = expr_switch.ComputeValue(content);//switch//

			for(int i = 1; i < listParam.Count - 1; i+=2){
				if(listParam[i] != null){
					//case xxx://
					if(switchVal.value.Equals(listParam[i].ComputeValue(content).value))
					{
						while(listParam[i + 1] == null){
							i+=2;
						}
//						content.InStack(listParam[i+1]);
						content.DepthAdd();
						listParam[i+1].ComputeValue(content);
						break;
					}else{
						continue;
					}
				}
				else{
					//default:
//					content.InStack(listParam[i+1]);
					content.DepthAdd();
					listParam[i+1].ComputeValue(content);
					break;
				}
			}

            content.DepthRemove();
            content.OutStack(this);
            return null;
        }
		public IEnumerator CoroutineCompute(CLS_Content content, ICoroutine coroutine)
		{
			content.InStack(this);
			content.DepthAdd();
			ICLS_Expression expr_switch = listParam[0] as ICLS_Expression;
			CLS_Content.Value switchVal = null;
			//			CLS_Content.Value vrt = null;
			if (expr_switch != null) 
				switchVal = expr_switch.ComputeValue(content);//switch//
			
			for(int i = 1; i < listParam.Count - 1; i+=2){
				if(listParam[i] != null){
					//case xxx://
					if(switchVal.value.Equals(listParam[i].ComputeValue(content).value))
					{
						while(listParam[i + 1] == null){
							i+=2;
						}
						content.DepthAdd();
						if(listParam[i+1].hasCoroutine){
							yield return coroutine.StartNewCoroutine(listParam[i+1].CoroutineCompute(content, coroutine));
						}else{
							listParam[i+1].ComputeValue(content);
						}
						break;
					}else{
						continue;
					}
				}
				else{
					//default:
					content.DepthAdd();
					if(listParam[i+1].hasCoroutine){
						yield return coroutine.StartNewCoroutine(listParam[i+1].CoroutineCompute(content, coroutine));
					}else{
						listParam[i+1].ComputeValue(content);
					}
					break;
				}
			}
			
			content.DepthRemove();
			content.OutStack(this);
		}

        public override string ToString()
        {
            return "SwitchCase|";
        }
    }
}