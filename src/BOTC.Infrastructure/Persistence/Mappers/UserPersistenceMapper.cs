using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence.Entities;

namespace BOTC.Infrastructure.Persistence.Mappers;

public static class UserPersistenceMapper
{
    public static UserEntity ToEntity(Domain.Users.User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserEntity
        {
            Id = user.Id.Value,
            Username = user.Username,
            NickName = user.NickName
        };
    }

    public static User ToDomain(UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return User.Rehydrate(
            new UserId(entity.Id),
            entity.Username,
            entity.NickName);
    }
}