using System;

namespace FBXSharp.ValueTypes
{
    public class Enumeration
    {
        private string[] m_flags;

        public int Value { get; set; }

        public string[] Flags
        {
            get => m_flags;
            set => m_flags = value ?? Array.Empty<string>();
        }

        public Enumeration()
        {
            Value = 0;
            m_flags = Array.Empty<string>();
        }

        public Enumeration(int value)
        {
            Value = value;
            m_flags = Array.Empty<string>();
        }

        public Enumeration(string[] flags)
        {
            Value = 0;
            m_flags = flags ?? Array.Empty<string>();
        }

        public Enumeration(int value, string[] flags)
        {
            Value = value;
            m_flags = flags ?? Array.Empty<string>();
        }
    }
}