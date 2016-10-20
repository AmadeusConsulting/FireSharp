using System.Collections;
using System.Collections.Generic;

namespace FireSharp.EventStreaming
{
    public delegate void ValueAddedEventHandler(object sender, ValueAddedEventArgs args, object context);

    public delegate void ValueRootAddedEventHandler<T>(object sender, T arg);

    public delegate void EntityAddedEventHandler<in TEntity>(object sender, string key, TEntity entity);

    public delegate void EntityChangedEventHandler<in TEntity>(object sender, string key, IEnumerable<string> paths, TEntity entity, TEntity oldValue);

    public delegate void EntityRemovedEventHandler<in TEntity>(object sender, string key, TEntity deleted);

    public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs args, object context);

    public delegate void ValueRemovedEventHandler(object sender, ValueRemovedEventArgs args, object context);
}