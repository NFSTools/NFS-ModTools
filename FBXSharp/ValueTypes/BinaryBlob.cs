using System;

namespace FBXSharp.ValueTypes
{
    public class BinaryBlob
    {
        private object[] m_datas;

        public object[] Datas
        {
            get => m_datas;
            set => m_datas = value ?? Array.Empty<object>();
        }

        public BinaryBlob()
        {
            m_datas = Array.Empty<object>();
        }

        public BinaryBlob(object[] datas)
        {
            m_datas = datas ?? Array.Empty<object>();
        }
    }
}