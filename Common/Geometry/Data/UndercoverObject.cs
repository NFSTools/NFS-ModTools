using System.Collections.Generic;

namespace Common.Geometry.Data
{
    public class UndercoverObject : SolidObject
    {
        public UndercoverObject()
        {
            TextureTypeList = new List<string>();
        }

        public List<string> TextureTypeList { get; set; }
    }
}