using System.Collections;
using EveAgain.SceneObjects;

namespace EveAgain.Render
{
    /// <summary>
    /// Summary description for RenderChain.
    /// </summary>
    public class RenderChain
    {
        public class RenderObject
        {
            public RenderObject()
            {
                Object = null;
                Enable = true;
            }

            public RenderObject(ISceneObject obj)
            {
                Object = obj;
                Enable = true;
            }

            public ISceneObject Object { get; set; }

            public bool Enable { get; set; }
        }

        private readonly Hashtable _objs;

        public RenderChain()
        {
            _objs = new Hashtable();
        }

        public RenderObject this[int index]
        {
            get => (RenderObject)_objs[index];
            set => _objs[index] = value;
        }

        public void Add(ISceneObject obj, int index)
        {
            _objs.Add(index, new RenderObject(obj));
        }
        public void Add(RenderObject obj, int index)
        {
            _objs.Add(index, obj);
        }
        public void Clear()
        {
            _objs.Clear();
        }
        public int Count => _objs.Count;
    }
}
