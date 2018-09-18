using System.IO;

namespace VltEd.VLT
{
    public abstract class VltDataBlock : IAddressable, IFileAccess
    {
        public abstract void Read(BinaryReader br);
        public abstract void Write(BinaryWriter bw);

        public long Address { get; set; }

        public ExpressionBlockBase Expression { get; set; }

        //public static VltDataBlock CreateOfType(VLTExpressionType type)
        //{
        //    switch (type)
        //    {
        //        case VLTExpressionType.DatabaseLoadData:
        //            return new VLTDataDatabaseLoad();
        //        case VLTExpressionType.ClassLoadData:
        //            return new VLTDataClassLoad();
        //        case VLTExpressionType.CollectionLoadData:
        //            return new VLTDataCollectionLoad();
        //        default:
        //            return null;
        //    }
        //}

        //public VLTDataDatabaseLoad AsDatabaseLoad()
        //{
        //    return this as VLTDataDatabaseLoad;
        //}
        //public VLTDataClassLoad AsClassLoad()
        //{
        //    return this as VLTDataClassLoad;
        //}

        public T AsCollectionLoad<T>() where T : CollectionLoadBase, new()
        {
            return this as T;
        }

        public VltDataDatabaseLoad AsDatabaseLoad()
        {
            return this as VltDataDatabaseLoad;
        }

    }
}
