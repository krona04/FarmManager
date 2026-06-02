using UnityEngine;

/// <summary>
/// In-game music manager.
///
/// Gameplay   → plays a shuffled/sequential playlist.
/// Shop/Talent/Build mode → cross-fades to that mode's AudioClip.
/// Returning to gameplay  → cross-fades BACK to the same track,
///                          resuming from the exact position it was at.
///
/// Assign in Inspector:
///   gameplayTracks[]  – song files for the normal gameplay loop
///   shopMusic         – plays when Seed Shop or Farm Upgrade Shop is open
///   talentMusic       – plays when Talent Screen is open  [T]
///   buildMusic        – plays when Build Menu / Build Mode is active
///
/// Leaving any mode clip null keeps the gameplay music uninterrupted.
/// </summary>
public class GameMusicManager : MonoBehaviour
{
    public static GameMusicManager Instance { get; private set; }

    [Header("Gameplay Playlist")]
    public AudioClip[] gameplayTracks;
    public bool        shuffle       = true;

    [Header("Mode Music  (null = keep gameplay music playing)")]
    public AudioClip shopMusic;
    public AudioClip talentMusic;
    public AudioClip buildMusic;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume      = 0.45f;
    public float                  fadeDuration = 1.2f;

    // ── Two sources for cross-fade (A = audible, B = fading in) ─────────────
    private AudioSource _srcA;
    private AudioSource _srcB;

    private bool  _fading;
    private float _fadeT;       // 0 → 1

    // ── State ─────────────────────────────────────────────────────────────────
    private enum Mode { Gameplay, Shop, Talent, Build }
    private Mode _mode     = Mode.Gameplay;
    private bool _modeInit;     // guard: don't react until Start() is done

    // ── Playlist ──────────────────────────────────────────────────────────────
    private int[]  _order;
    private int    _pos;
    private float  _savedPos;   // playback position saved when leaving Gameplay

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _srcA = AddSrc();
        _srcB = AddSrc();

        BuildOrder();
    }

    private void Start()
    {
        volume  = SettingsManager.MusicVolume;
        shuffle = SettingsManager.ShufflePlaylist;
        BuildOrder();

        if (HasPlaylist())
        {
            _srcA.clip   = Track(_pos);
            _srcA.loop   = false;
            _srcA.volume = volume;
            _srcA.Play();
        }
        _modeInit = true;
    }

    private void Update()
    {
        if (!_modeInit) return;

        // ── Mode change detection ─────────────────────────────────────────────
        Mode target = Resolve();
        if (target != _mode)
        {
            // Before leaving Gameplay: snapshot the current playback time
            if (_mode == Mode.Gameplay && !_fading
                && _srcA != null && _srcA.clip != null && _srcA.isPlaying)
            {
                _savedPos = _srcA.time;
            }

            _mode = target;

            if (target == Mode.Gameplay)
            {
                // Resume the same playlist track at the saved position
                ResumeGameplay();
            }
            else
            {
                AudioClip modeClip = ClipFor(target);
                if (modeClip != null)
                    CrossFadeTo(modeClip, loop: true);
                // else: mode has no special clip → keep gameplay music as-is
            }
        }

        // ── Cross-fade progress ────────────────────────────────────────────────
        if (_fading)
        {
            _fadeT = Mathf.MoveTowards(_fadeT, 1f,
                         Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));

            _srcA.volume = volume * (1f - _fadeT);
            _srcB.volume = volume * _fadeT;

            if (Mathf.Approximately(_fadeT, 1f))
            {
                _srcA.Stop();
                Swap();                     // B becomes A (the new active source)
                _srcA.volume = volume;
                _srcB.volume = 0f;
                _fading      = false;
                _fadeT       = 0f;
            }
        }

        // ── Auto-advance playlist when a track finishes ───────────────────────
        if (!_fading && _mode == Mode.Gameplay && HasPlaylist() && !_srcA.isPlaying)
        {
            _pos = (_pos + 1) % _order.Length;
            _srcA.clip = Track(_pos);
            _srcA.Play();
        }
    }

    // ── Resume gameplay at saved position ─────────────────────────────────────

    private void ResumeGameplay()
    {
        if (!HasPlaylist()) return;

        AudioClip clip = Track(_pos);
        if (clip == null) return;

        // Clamp saved position in case the clip is shorter than expected
        float at = (_savedPos < clip.length) ? _savedPos : 0f;

        _srcB.Stop();
        _srcB.clip   = clip;
        _srcB.loop   = false;
        _srcB.volume = 0f;
        _srcB.time   = at;       // resume from where we paused
        _srcB.Play();

        _fadeT  = 0f;
        _fading = true;
    }

    // ── Generic cross-fade ────────────────────────────────────────────────────

    private void CrossFadeTo(AudioClip clip, bool loop)
    {
        _srcB.Stop();
        _srcB.clip   = clip;
        _srcB.loop   = loop;
        _srcB.volume = 0f;
        _srcB.time   = 0f;
        if (clip != null) _srcB.Play();

        _fadeT  = 0f;
        _fading = true;
    }

    private void Swap()
    {
        var tmp = _srcA; _srcA = _srcB; _srcB = tmp;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Mode Resolve()
    {
        if (ShopMenu.IsShopOpen || FarmUpgradeShop.IsOpen) return Mode.Shop;
        if (TalentScreen.IsOpen)                           return Mode.Talent;
        if (BuildManager.IsBuildMenuOpen || BuildManager.IsBuildModeActive) return Mode.Build;
        return Mode.Gameplay;
    }

    private AudioClip ClipFor(Mode m) => m switch
    {
        Mode.Shop    => shopMusic,
        Mode.Talent  => talentMusic,
        Mode.Build   => buildMusic,
        _            => null
    };

    private bool      HasPlaylist()     => gameplayTracks != null && gameplayTracks.Length > 0;
    private AudioClip Track(int i)      => HasPlaylist() ? gameplayTracks[_order[i]] : null;

    private void BuildOrder()
    {
        int n  = HasPlaylist() ? gameplayTracks.Length : 0;
        _order = new int[Mathf.Max(n, 1)];
        for (int i = 0; i < n; i++) _order[i] = i;

        if (shuffle && n > 1)
        {
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }
        _pos = 0;
    }

    private AudioSource AddSrc()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake  = false;
        s.loop         = false;
        s.volume       = 0f;
        s.spatialBlend = 0f;
        return s;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (!_fading) { _srcA.volume = volume; _srcB.volume = 0f; }
    }
}
