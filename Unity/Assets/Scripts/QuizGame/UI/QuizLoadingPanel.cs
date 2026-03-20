using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

/// <summary>
/// 퀴즈 로딩 패널.
/// 패널 표시·숨김 및 도트 애니메이션을 내부에서 관리한다.
/// </summary>
public class QuizLoadingPanel
{
    private readonly VisualElement   _panel;
    private readonly VisualElement[] _dots;
    private CancellationTokenSource  _dotCts;

    public QuizLoadingPanel(VisualElement root)
    {
        _panel = root.Q<VisualElement>("loading-panel");
        _dots  = new VisualElement[]
        {
            root.Q("dot-0"),
            root.Q("dot-1"),
            root.Q("dot-2"),
        };
    }

    public void Show()
    {
        if (_panel != null) _panel.RemoveFromClassList("hidden");
        StartDotAnimation();
    }

    public void Hide()
    {
        if (_panel != null) _panel.AddToClassList("hidden");
        StopDotAnimation();
    }

    // ── 도트 애니메이션 ────────────────────────────────────
    private void StartDotAnimation()
    {
        StopDotAnimation();
        _dotCts = new CancellationTokenSource();
        int[] offsets = { 0, 250, 500 };
        for (int i = 0; i < _dots.Length; i++)
        {
            if (_dots[i] != null)
                AnimateDotAsync(_dots[i], offsets[i], _dotCts.Token).Forget();
        }
    }

    private async UniTaskVoid AnimateDotAsync(VisualElement dot, int initialDelayMs, CancellationToken ct)
    {
        try
        {
            if (initialDelayMs > 0)
                await UniTask.Delay(initialDelayMs, cancellationToken: ct);

            while (!ct.IsCancellationRequested)
            {
                dot.AddToClassList("active");
                await UniTask.Delay(400, cancellationToken: ct);
                dot.RemoveFromClassList("active");
                await UniTask.Delay(350, cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void StopDotAnimation()
    {
        _dotCts?.Cancel();
        _dotCts?.Dispose();
        _dotCts = null;
        if (_dots == null) return;
        foreach (var dot in _dots)
            dot?.RemoveFromClassList("active");
    }
}
