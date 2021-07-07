namespace Veauty.Patch
{
    public class Attach<T> : IPatch<T>
    {
        private T target;
        private readonly int index;
        public readonly System.Type oldComponent;
        public readonly System.Type newComponent;

        public Attach(int index, System.Type oldComponent, System.Type newComponent)
        {
            this.index = index;
            this.oldComponent = oldComponent;
            this.newComponent = newComponent;
            this.target = default(T);
        }
        
        public T GetTarget() => this.target;

        public void SetTarget(in T target) => this.target = target;

        public int GetIndex() => this.index;
    }
}