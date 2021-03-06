﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CQuark
{
    class Type_NULL : IType
    {
        public string keyword
        {
            get { return "null"; }
        }
        public string _namespace
        {
            get { return ""; }
        }
        public TypeBridge typeBridge
        {
            get { return null; }
        }
        public object defaultValue
        {
            get { return null; }
        }
		public IClass _class
        {
            get { throw new NotImplementedException(); }
        }

        public object ConvertTo(object src, TypeBridge targetType)
        {
            return null;
        }
        public CQ_Value Math2Value (char code, CQ_Value left, CQ_Value right) {
            if (right.m_type == typeof(string))
            {
                CQ_Value returnValue = new CQ_Value();
                returnValue.SetObject(typeof(string), "null" + right.GetObject());
                return returnValue;
            }
            throw new NotImplementedException();
        }
        public bool MathLogic(LogicToken code, CQ_Value left, CQ_Value right)
        {
            if (code == LogicToken.equal)
            {
                return null == right.GetObject();
            }
            else if(code== LogicToken.not_equal)
            {
                return null != right.GetObject();
            }
            throw new NotImplementedException();
        }
    }
}
