using System.Collections.Generic;

namespace FireSharp
{
    public interface IDatabaseRules
    {
        #region Public Properties

        IDictionary<string, object> Rules { get; }

        #endregion

        #region Public Indexers

        IDatabaseRules this[string key] { get; set; }

        #endregion

        void Remove(string key);
    }
}