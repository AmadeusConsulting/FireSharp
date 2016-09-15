using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace FireSharp
{
    [JsonConverter(typeof(DatabaseRulesSerializer))]
    public class DatabaseRules
    {
        #region Constructors and Destructors

        public DatabaseRules(IDictionary<string, object> rules)
        {
            Rules = rules;
        }

        #endregion

        #region Public Properties

        public IDictionary<string, object> Rules { get; }

        #endregion

        #region Public Indexers

        public DatabaseRules this[string key]
        {
            get
            {
                return GetChild(key);
            }

            set
            {
                Rules[key] = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static DatabaseRules Create(IDictionary<string, dynamic> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return new DatabaseRules(rules);
        }

        public static implicit operator DatabaseRules(Dictionary<string, object> dict)
        {
            if (dict == null)
            {
                return null;
            }

            return new DatabaseRules(dict);
        }

        public void Remove(string key)
        {
            Rules.Remove(key);
        }

        #endregion

        #region Methods

        private DatabaseRules GetChild(string key)
        {
            if (!Rules.ContainsKey(key))
            {
                Rules[key] = new Dictionary<string, object>();
            }

            return new DatabaseRules((IDictionary<string, object>)Rules[key]);
        }

        #endregion
    }
}