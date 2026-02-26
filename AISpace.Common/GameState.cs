namespace AISpace.Common;

public class GameState(int worldID, int channelID)
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _worldID = worldID;
    private readonly int _channelID = channelID;
    private readonly Dictionary<Guid, string> _players = new Dictionary<Guid, string>();
    private readonly Dictionary<Guid, object> _npcs = new Dictionary<Guid, object>();

    public void AddPlayer(Guid id, string name)
    {
        _lock.EnterWriteLock();
        try
        {
            _players[id] = name;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int getWorldID()
    {
        return _worldID;
    }

    public int getChannelID()
    {
        return _channelID;
    }

    public void RemovePlayer(Guid id)
    {
        _lock.EnterWriteLock();
        try
        {
            _players.Remove(id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IReadOnlyDictionary<Guid, string> SnapshotPlayers()
    {
        _lock.EnterReadLock();
        try
        {
            return new Dictionary<Guid, string>(_players);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
