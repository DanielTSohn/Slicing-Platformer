using System;
public interface IGameEvent
{
    public event Action OnGameEventTriggered;
}