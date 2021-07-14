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

        public override bool Equals(object obj) => this.Equals(obj as Attach<T>);

        public bool Equals(Attach<T> obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }


            return this.index == obj.index && this.oldComponent == obj.oldComponent && this.newComponent == obj.newComponent;
        }

        public override int GetHashCode()
        {
            return new { index, oldComponent, newComponent }.GetHashCode();
        }
    }
}