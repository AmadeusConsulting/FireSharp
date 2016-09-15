using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace FireSharp
{
    [JsonConverter(typeof(DatabaseRulesSerializer))]
    public class DatabaseRules : IDatabaseRules
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

        public IDatabaseRules this[string key]
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

        public static IDatabaseRules Create(IDictionary<string, object> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return new DatabaseRules(rules);
        }

        public void Remove(string key)
        {
            Rules.Remove(key);
        }

        #endregion

        #region Methods

        private IDatabaseRules GetChild(string path)
        {
            var splitPath = path.Split('/');

            var currentRule = Rules;

            foreach (var elem in splitPath)
            {
                if (!currentRule.ContainsKey(elem))
                {
                    currentRule[elem] = new Dictionary<string, object>();
                }

                currentRule = (IDictionary<string, object>)currentRule[elem];
            }

            return new DatabaseRules(currentRule);
        }

        #endregion
    }
}