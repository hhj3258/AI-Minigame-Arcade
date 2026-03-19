using Cysharp.Threading.Tasks;

public interface IMinigame
{
    UniTask InitializeAsync();
    void OnGameStart();
    void OnGameEnd();
}

