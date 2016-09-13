using System;
using System.Threading.Tasks;
using FireSharp.EventStreaming;
using FireSharp.Response;

namespace FireSharp.Interfaces
{
    public interface IFirebaseClient
    {
        Task<FirebaseResponse> GetAsync(string path);
        Task<FirebaseResponse> GetAsync(string path, QueryBuilder queryBuilder);

        Task<IEventStreamResponse> OnChangeGetAsync<T>(string path, ValueRootAddedEventHandler<T> added = null);
        Task<SetResponse> SetAsync<T>(string path, T data);
        Task<PushResponse> PushAsync<T>(string path, T data);
        Task<FirebaseResponse> DeleteAsync(string path);
        Task<FirebaseResponse> UpdateAsync<T>(string path, T data);
        FirebaseResponse Get(string path, QueryBuilder queryBuilder);
        FirebaseResponse Get(string path);
        SetResponse Set<T>(string path, T data);
        PushResponse Push<T>(string path, T data);
        FirebaseResponse Delete(string path);
        FirebaseResponse Update<T>(string path, T data);
        FirebaseResponse CreateUser(string email, string password);
        FirebaseResponse ChangeEmail(string oldEmail, string password, string newEmail);
        FirebaseResponse RemoveUser(string email, string password);
        FirebaseResponse ResetPassword(string email, string password);
        FirebaseResponse ChangePassword(string email, string oldPassword, string newPassword);

        [Obsolete("This method is obsolete use OnAsync instead.")]
        Task<IEventStreamResponse> ListenAsync(string path,
            ValueAddedEventHandler added = null,
            ValueChangedEventHandler changed = null,
            ValueRemovedEventHandler removed = null);

        Task<IEventStreamResponse> OnAsync(
            string path,
            ValueAddedEventHandler added = null,
            ValueChangedEventHandler changed = null,
            ValueRemovedEventHandler removed = null,
            object context = null);

        Task<IEventStreamResponse> MonitorEntityListAsync<TEntity>(
            string path,
            EntityAddedEventHandler<TEntity> added,
            EntityChangedEventHandler<TEntity> changed,
            EntityRemovedEventHandler<TEntity> removed,
            QueryBuilder queryBuilder = null);

        Task<dynamic> GetDatabaseRulesAsync();

        Task<SetResponse> SetDatabaseRulesAsync(dynamic rules);
    }
}