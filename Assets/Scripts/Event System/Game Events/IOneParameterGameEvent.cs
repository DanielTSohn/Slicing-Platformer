using System;
public interface IOneParameterGameEvent<T>
{
    public event Action<T> OnOneParameterEventTriggered;
}