namespace BGME.Framework;

public interface IBgmeService : IGameHook
{
    void SetVictoryDisabled(bool isDisabled);
}
