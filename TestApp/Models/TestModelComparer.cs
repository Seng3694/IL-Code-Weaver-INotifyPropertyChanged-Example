using System.Collections.Generic;

namespace TestApp.Model
{
    public class TestModelComparer : IEqualityComparer<TestModel>
    {
        private static TestModelComparer _default;

        public static TestModelComparer Default
        {
            get
            {
                if (_default == null)
                    _default = new TestModelComparer();

                return _default;
            }
        }

        private TestModelComparer() { }

        public bool Equals(TestModel x, TestModel y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false; 
            return x.TestIntProperty == y.TestIntProperty && x.TestStringProperty == y.TestStringProperty;
        }

        public int GetHashCode(TestModel obj)
        {
            return obj?.TestIntProperty.GetHashCode() ?? 0 + obj?.TestStringProperty.GetHashCode() ?? 0;
        }
    }
}
