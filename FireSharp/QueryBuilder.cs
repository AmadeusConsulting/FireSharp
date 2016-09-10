using System;
using System.Collections.Generic;
using System.Linq;

namespace FireSharp
{
    public class QueryBuilder
    {
        private readonly string _initialQuery;
        private string formatParam = "format";
        private string shallowParam = "shallow";
        private string orderByParam = "orderBy";
        private string startAtParam = "startAt";
        private string endAtParam = "endAt";
        private string equalToParam = "equalTo";
        private string formatVal = "export";
        private string limitToFirstParam = "limitToFirst";
        private string limitToLastParam = "limitToLast";

        static Dictionary<string, object> _query = new Dictionary<string, object>();

        private QueryBuilder(string initialQuery = "")
        {
            _initialQuery = initialQuery;
            _query = new Dictionary<string, object>();
        }

        public static QueryBuilder New(string initialQuery = "")
        {
            return new QueryBuilder(initialQuery);
        }
        public QueryBuilder StartAt(object value)
        {
            return AddToQueryDictionary(startAtParam, value, skipQuotesForNonString: true);
        }

        public QueryBuilder EndAt(object value)
        {
            return AddToQueryDictionary(endAtParam, value, skipQuotesForNonString: true);
        }
        
        public QueryBuilder EqualTo(object value)
        {
            return AddToQueryDictionary(equalToParam, value, skipQuotesForNonString: true);
        }

        public QueryBuilder OrderBy(string value)
        {
            return AddToQueryDictionary(orderByParam, value);
        }

        public QueryBuilder LimitToFirst(int value)
        {
            return AddToQueryDictionary(limitToFirstParam, value > 0 ? value.ToString() : string.Empty, skipEncoding: true);
        }

        public QueryBuilder LimitToLast(int value)
        {
            return AddToQueryDictionary(limitToLastParam, value > 0 ? value.ToString() : string.Empty, skipEncoding: true);
        }
        
        public QueryBuilder Shallow(bool value)
        {
            return AddToQueryDictionary(shallowParam, value ? "true" : string.Empty, skipEncoding: true);
        }

        public QueryBuilder IncludePriority(bool value)
        {
            return AddToQueryDictionary(formatParam, value ? formatVal : string.Empty, skipEncoding: true);
        }

        private QueryBuilder AddToQueryDictionary(string parameterName, object value, bool skipEncoding = false, bool skipQuotesForNonString = false)
        {
            if (value != null)
            {
                var quote = !skipQuotesForNonString || (value is string);

                var stringValue = value.ToString();

                if (!string.IsNullOrEmpty(stringValue))
                {
                    _query.Add(parameterName, skipEncoding ? value : EscapeString(stringValue, quote));
                    return this;
                }
            }

            _query.Remove(startAtParam);

            return this;
        }

        private string EscapeString(string value, bool quote = true)
        {
            var quotes = quote ? "\"" : string.Empty;
            return $"{quotes}{Uri.EscapeDataString(value).Replace("%20", "+").Trim('\"')}{quotes}";
        }

        public string ToQueryString()
        {
            if (!_query.Any() && !string.IsNullOrEmpty(_initialQuery)) return _initialQuery;

            return !string.IsNullOrEmpty(_initialQuery)
                ? $"{_initialQuery}&{string.Join("&", _query.Select(pair => $"{pair.Key}={pair.Value}").ToArray())}"
                : string.Join("&", _query.Select(pair => $"{pair.Key}={pair.Value}").ToArray());
        }
    }
}
