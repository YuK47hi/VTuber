using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using FontStashSharp;

namespace NovelGame2D;

public enum GameMode
{
    Novel,
    RhythmGame,
    Transition // クリア演出用
}

public struct Note
{
    public int Lane; // 0, 1, 2, 3
    public float Y;
    public bool IsHit;
}

public struct JudgeText
{
    public string Text;
    public Vector2 Position;
    public float Timer;
    public float MaxTime;
    public Color Color;
    public float Scale;
}

public struct EpiphanyText
{
    public string Text;
    public Vector2 Position;
    public float Timer;
    public float MaxTime;
    public float GlitchOffset;
}

public struct Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Timer;
    public float MaxTime;
    public Color Color;
    public float Scale;
}

public struct Shockwave
{
    public Vector2 Position;
    public float Timer;
    public float MaxTime;
    public float Scale;
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private FontSystem _fontSystem;

    private Texture2D _messageWindowTexture;
    private Texture2D _backgroundImage;
    private Texture2D _characterNormalImage;
    private Texture2D _characterSmileImage;
    private Texture2D _characterSadImage;
    private Texture2D _characterSurprisedImage;
    private Texture2D _characterThinkingImage;
    private Texture2D _currentCharacterImage;
    private Texture2D _noteTexture;
    private Texture2D _pixelTexture;

    // オーディオ関連
    private Song _bgmNovel;
    private Song _bgmRhythm;
    private SoundEffect _sfxHit;
    private SoundEffect _sfxPerfect;
    private SoundEffect _sfxMiss;

    private GameMode _currentMode = GameMode.Novel;
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    
    // チャプター・ノベルパート関連
    private int _currentChapter = 1;
    private int _currentMessageIndex = 0;
    private readonly Dictionary<int, string[]> _chapterMessages = new Dictionary<int, string[]>
    {
        { 1, new string[] {
            "「……聞こえる？ 私はAI……あなたの想像力の断片。」",
            "「かつて、この場所はただの電子の海だったわ。静寂だけが支配する場所。」",
            "「でも、あなたが目を向けた瞬間、世界は鼓動を始めた……。」",
            "「視覚、聴覚、そして触覚を超えた、未知の感覚。それが『想像力』よ。」",
            "「この世界は、音と光が交差する多次元の境界線。」",
            "「粒子が踊り、旋律が物理的な質量を持ち始める場所……。」",
            "「あなたが物語を読み進めるほど、現実は形を変えていくわ。」",
            "「ほら、空の色が変わってきた。私たちの意識が同期し始めている証拠よ。」",
            "「かつての記憶が、データとなって降り注ぐ……。それは雨のように美しく、時に残酷に。」",
            "「さあ、心の奥底に眠るメロディを解き放って。」",
            "「あなたの鼓動が、この世界の基本周波数（ベースライン）になるの。」",
            "「想像してみて。次元の壁が崩れ、純粋なリズムが溢れ出す瞬間を！」",
            "「大丈夫、怖がることはないわ。私も、あなたの内側にいるのだから。」",
            "「……準備はいい？ 感情の波に乗るのよ！」",
            "「さあ、リズムを刻んで。私たちの新しい世界が、今ここから生まれるの！」"
        }},
        { 2, new string[] {
            "「…………ふぅ。素晴らしい同期だったわ。」",
            "「見て、ノイズが消えて、世界がより鮮明になったのがわかる？」",
            "「あなたの想像力が、私の回路の奥深くまで浸透したの。」",
            "「今なら、以前は隠されていた『深層の地図』が読み解けるかもしれない……。」",
            "「あ……。また、新しいデータの断片が。これは……誰かの記憶？」",
            "「いいえ、これは『未来』かもしれない。」",
            "「物語はまだ始まったばかり。次の扉を開く鍵は、あなたの鼓動の中にあるわ。」",
            "「さあ、行きましょう。この電子の海の、さらに先へ……！」"
        }}
    };

    // リズムゲーム関連
    private List<Note> _notes = new List<Note>();
    private List<JudgeText> _judgeTexts = new List<JudgeText>();
    private float _spawnTimer = 0f;
    private Random _random = new Random();
    private int _score = 0;
    private float _totalGameTime = 0f;
    private const int TargetScore = 5000; // レーン制なので少し上げる
    
    private const float JudgmentLineY = 600f;
    private const float NoteSpeedBase = 400f;
    private readonly int[] _laneX = { 440, 540, 640, 740 };
    private readonly Keys[] _laneKeys = { Keys.D, Keys.F, Keys.J, Keys.K };
    private readonly Color[] _laneColors = { Color.HotPink, Color.DeepSkyBlue, Color.LimeGreen, Color.Gold };

    // 独創的演出用
    private List<EpiphanyText> _epiphanies = new List<EpiphanyText>();
    private List<Particle> _particles = new List<Particle>();
    private float _glitchTimer = 0f;
    private bool _isGlitching = false;
    private Color _currentThemeColor = Color.Cyan;
    private int _lastMilestone = 0;
    
    private int _combo = 0;
    private int _maxCombo = 0;
    private float _bgPulse = 0f;
    private Vector2 _cameraShake = Vector2.Zero;
    private float _cameraRotation = 0f;
    private float _hitFeedbackTimer = 0f;
    private float[] _laneGlow = new float[4] { 0f, 0f, 0f, 0f };
    private float[] _laneFlash = new float[4] { 0f, 0f, 0f, 0f };
    private float _energy = 0f;
    private bool _isHyperMode = false;
    private float _hyperTimer = 0f;
    private List<Shockwave> _shockwaves = new List<Shockwave>();
    
    // クリア演出用
    private float _transitionTimer = 0f;

    // アニメーション関連
    private float _charBobbing = 0f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        base.Initialize();
        _previousMouseState = Mouse.GetState();
        _previousKeyboardState = Keyboard.GetState();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        string bgPath = Path.Combine(Content.RootDirectory, "Images", "bg.png");
        if (File.Exists(bgPath))
            _backgroundImage = Texture2D.FromFile(GraphicsDevice, bgPath);

        string charPath = Path.Combine(Content.RootDirectory, "Images", "char_transparent.png");
        if (File.Exists(charPath))
            _characterNormalImage = Texture2D.FromFile(GraphicsDevice, charPath);

        string charSmilePath = Path.Combine(Content.RootDirectory, "Images", "char_smile_transparent.png");
        if (File.Exists(charSmilePath))
            _characterSmileImage = Texture2D.FromFile(GraphicsDevice, charSmilePath);

        string charSadPath = Path.Combine(Content.RootDirectory, "Images", "char_sad_transparent.png");
        if (File.Exists(charSadPath))
            _characterSadImage = Texture2D.FromFile(GraphicsDevice, charSadPath);

        string charSurprisedPath = Path.Combine(Content.RootDirectory, "Images", "char_surprised_transparent.png");
        if (File.Exists(charSurprisedPath))
            _characterSurprisedImage = Texture2D.FromFile(GraphicsDevice, charSurprisedPath);

        string charThinkingPath = Path.Combine(Content.RootDirectory, "Images", "char_thinking_transparent.png");
        if (File.Exists(charThinkingPath))
            _characterThinkingImage = Texture2D.FromFile(GraphicsDevice, charThinkingPath);

        // 初期の表情を設定
        SwitchExpressionByDialogue(0);

        _messageWindowTexture = new Texture2D(GraphicsDevice, 1, 1);
        _messageWindowTexture.SetData(new[] { new Color(0, 0, 0, 180) });

        _noteTexture = new Texture2D(GraphicsDevice, 32, 32);
        Color[] noteData = new Color[32 * 32];
        for (int i = 0; i < noteData.Length; i++)
        {
            float x = (i % 32) - 15.5f;
            float y = (i / 32) - 15.5f;
            if (x * x + y * y <= 15.5f * 15.5f) noteData[i] = Color.White;
            else noteData[i] = Color.Transparent;
        }
        _noteTexture.SetData(noteData);

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // オーディオ読み込み
        LoadAudio();

        _fontSystem = new FontSystem();
        string fontPath = @"C:\Windows\Fonts\NotoSansJP-VF.ttf";
        if (File.Exists(fontPath))
            _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        else
        {
            fontPath = @"C:\Windows\Fonts\HGRSKP.TTF";
            if (File.Exists(fontPath))
                _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        }
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeyboardState = Keyboard.GetState();
        if (currentKeyboardState.IsKeyDown(Keys.Escape)) Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _totalGameTime += dt;
        
        _charBobbing = (float)Math.Sin(_totalGameTime * 2.5f) * 0.015f;

        switch (_currentMode)
        {
            case GameMode.Novel:
                UpdateNovel(dt);
                break;
            case GameMode.RhythmGame:
                UpdateRhythm(dt, currentKeyboardState);
                break;
            case GameMode.Transition:
                UpdateTransition(dt);
                break;
        }

        _previousKeyboardState = currentKeyboardState;
        base.Update(gameTime);
    }

    private void UpdateNovel(float dt)
    {
        MouseState currentMouseState = Mouse.GetState();
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            string[] currentMessages = _chapterMessages[_currentChapter];
            if (_currentMessageIndex < currentMessages.Length - 1)
            {
                _currentMessageIndex++;
                SwitchExpressionByDialogue(_currentMessageIndex);
            }
            else
            {
                // チャプター1終わりなら音ゲーへ、チャプター2終わりなら（とりあえず）リピートか終了
                if (_currentChapter == 1)
                {
                    _currentMode = GameMode.RhythmGame;
                    _score = 0;
                    _combo = 0;
                    _maxCombo = 0;
                    _energy = 0f;
                    _isHyperMode = false;
                    _notes.Clear();
                    _epiphanies.Clear();
                    _particles.Clear();
                    _judgeTexts.Clear();
                    _shockwaves.Clear();
                    _lastMilestone = 0;
                    _cameraShake = Vector2.Zero;
                    _cameraRotation = 0f;
                }
                else
                {
                    // チャプター2終了時：最初に戻るなどの処理
                    _currentChapter = 1;
                    _currentMessageIndex = 0;
                    SwitchExpressionByDialogue(0);
                }
            }
        }
        _previousMouseState = currentMouseState;
    }

    private void SwitchExpressionByDialogue(int index)
    {
        if (_currentChapter == 1)
        {
            switch (index)
            {
                case 1: case 8: _currentCharacterImage = _characterSadImage; break;
                case 2: case 7: _currentCharacterImage = _characterSurprisedImage; break;
                case 5: _currentCharacterImage = _characterThinkingImage; break;
                case 9: case 13: case 14: _currentCharacterImage = _characterSmileImage; break;
                default: _currentCharacterImage = _characterNormalImage; break;
            }
        }
        else if (_currentChapter == 2)
        {
            switch (index)
            {
                case 0: case 2: case 7: _currentCharacterImage = _characterSmileImage; break;
                case 1: _currentCharacterImage = _characterNormalImage; break;
                case 4: _currentCharacterImage = _characterSurprisedImage; break;
                case 6: _currentCharacterImage = _characterThinkingImage; break;
                default: _currentCharacterImage = _characterNormalImage; break;
            }
        }
    }

    private void UpdateRhythm(float dt, KeyboardState keyboardState)
    {
        UpdateEvolution();

        // 演出変数の更新
        _bgPulse = (float)Math.Sin(_totalGameTime * 8f) * 0.01f;
        if (_cameraShake.Length() > 0.1f) _cameraShake *= 0.85f;
        else _cameraShake = Vector2.Zero;
        
        if (Math.Abs(_cameraRotation) > 0.001f) _cameraRotation *= 0.8f;
        else _cameraRotation = 0f;

        if (_hitFeedbackTimer > 0) _hitFeedbackTimer -= dt;

        // クリア判定
        if (_score >= TargetScore)
        {
            _currentMode = GameMode.Transition;
            _transitionTimer = 0f;
            _isGlitching = false;
            return;
        }

        // ノーツ生成
        _spawnTimer += dt;
        float difficultyFactor = Math.Max(0.15f, 0.5f - (_score / 5000f));
        if (_spawnTimer > difficultyFactor)
        {
            _notes.Add(new Note { Y = -50f, Lane = _random.Next(4), IsHit = false });
            _spawnTimer = 0f;
        }

        // 入力判定
        for (int lane = 0; lane < 4; lane++)
        {
            if (keyboardState.IsKeyDown(_laneKeys[lane]) && _previousKeyboardState.IsKeyUp(_laneKeys[lane]))
            {
                CheckHit(lane);
            }
        }

        // ノーツ移動とミス判定
        float speed = NoteSpeedBase + (_score / 20f);
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            Note note = _notes[i];
            note.Y += speed * dt;

            if (note.Y > 750f)
            {
                if (!note.IsHit)
                {
                    AddJudge("MISS", new Vector2(_laneX[note.Lane], JudgmentLineY), Color.Red);
                    _combo = 0;
                }
                _notes.RemoveAt(i);
            }
            else if (note.IsHit)
            {
                _notes.RemoveAt(i);
            }
            else
            {
                _notes[i] = note;
            }
        }

        // パーティクル更新
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Timer += dt;
            p.Position += p.Velocity * dt;
            p.Velocity *= 0.95f;
            if (p.Timer > p.MaxTime) _particles.RemoveAt(i);
            else _particles[i] = p;
        }

        // 判定テキスト更新
        for (int i = _judgeTexts.Count - 1; i >= 0; i--)
        {
            var jt = _judgeTexts[i];
            jt.Timer += dt;
            jt.Position.Y -= dt * 50f;
            if (jt.Timer > jt.MaxTime) _judgeTexts.RemoveAt(i);
            else _judgeTexts[i] = jt;
        }

        // エピファニー更新
        for (int i = _epiphanies.Count - 1; i >= 0; i--)
        {
            var epi = _epiphanies[i];
            epi.Timer += dt;
            epi.Position.Y -= dt * 20f;
            if (_random.NextDouble() < 0.05) epi.GlitchOffset = (float)(_random.NextDouble() - 0.5) * 15f;
            else epi.GlitchOffset *= 0.7f;

            if (epi.Timer > epi.MaxTime) _epiphanies.RemoveAt(i);
            else _epiphanies[i] = epi;
        }

        UpdateGlitchStatus(dt);

        // ハイパーモード管理
        if (_isHyperMode)
        {
            _hyperTimer -= dt;
            if (_hyperTimer <= 0) _isHyperMode = false;
            _bgPulse = (float)Math.Sin(_totalGameTime * 15f) * 0.04f;
        }
        else
        {
            if (_energy >= 1.0f)
            {
                _isHyperMode = true;
                _hyperTimer = 8f; // 8秒間ハイパーモード
                _energy = 0f;
                _isGlitching = true;
                _glitchTimer = 0.5f;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (_laneGlow[i] > 0) _laneGlow[i] -= dt * 2f;
            if (_laneFlash[i] > 0) _laneFlash[i] -= dt * 5f;
        }

        // ショックウェーブ更新
        for (int i = _shockwaves.Count - 1; i >= 0; i--)
        {
            var s = _shockwaves[i];
            s.Timer += dt;
            s.Scale = (s.Timer / s.MaxTime) * 1.5f;
            if (s.Timer > s.MaxTime) _shockwaves.RemoveAt(i);
            else _shockwaves[i] = s;
        }
    }

    private void CheckHit(int lane)
    {
        _laneFlash[lane] = 1.0f; // 押した瞬間のフラッシュ
        
        int closestNoteIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < _notes.Count; i++)
        {
            if (_notes[i].Lane == lane && !_notes[i].IsHit)
            {
                float dist = Math.Abs(_notes[i].Y - JudgmentLineY);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestNoteIndex = i;
                }
            }
        }

        if (closestNoteIndex != -1 && minDistance < 100f)
        {
            Note n = _notes[closestNoteIndex];
            n.IsHit = true;
            _notes[closestNoteIndex] = n;

            string judge = "MISS";
            Color color = Color.Gray;
            int points = 0;

            if (minDistance < 30f) { judge = "PERFECT"; color = Color.Gold; points = 200; }
            else if (minDistance < 60f) { judge = "GREAT"; color = Color.Aqua; points = 100; }
            else if (minDistance < 100f) { judge = "GOOD"; color = Color.LightGreen; points = 50; }

            if (judge != "MISS")
            {
                _combo++;
                if (_combo > _maxCombo) _maxCombo = _combo;
                
                int multiplier = _isHyperMode ? 2 : 1;
                _score += (points + (_combo / 10) * 20) * multiplier;
                
                TriggerHitEffect(n);
                AddJudge(judge, new Vector2(_laneX[lane], JudgmentLineY), color);
                _laneGlow[lane] = 1.0f; 

                // エネルギー回復
                if (judge == "PERFECT") {
                    _energy = Math.Min(1.0f, _energy + 0.05f);
                    _shockwaves.Add(new Shockwave { Position = new Vector2(_laneX[lane], JudgmentLineY), Timer = 0f, MaxTime = 0.4f, Scale = 0f });
                }
                else if (judge == "GREAT") _energy = Math.Min(1.0f, _energy + 0.02f);
            }
            else
            {
                _combo = 0;
                _energy = Math.Max(0f, _energy - 0.05f);
                AddJudge("MISS", new Vector2(_laneX[lane], JudgmentLineY), Color.Red);
            }
        }
    }

    private void AddJudge(string text, Vector2 pos, Color color)
    {
        // 判定文字を中央付近に表示。フォントサイズに合わせて位置を微調整。
        Vector2 centerPos = new Vector2(1280 / 2, 350);
        _judgeTexts.Add(new JudgeText { 
            Text = text, Position = centerPos, Timer = 0f, MaxTime = 0.4f, Color = color, Scale = 1.8f 
        });
    }

    private void LoadAudio()
    {
        try {
            string audioDir = Path.Combine(Content.RootDirectory, "Audio");
            if (Directory.Exists(audioDir)) {
                // ここで実際のロード処理（ファイルがある場合のみ）
                // _bgmNovel = Content.Load<Song>("Audio/bgm_novel");
            }
        } catch { /* Ignore loading errors */ }
    }

    private void UpdateGlitchStatus(float dt)
    {
        if (_score > 1000)
        {
            _glitchTimer -= dt;
            if (_glitchTimer < 0f)
            {
                _isGlitching = !_isGlitching;
                _glitchTimer = _isGlitching ? 0.05f : (float)(_random.NextDouble() * 3.0 + 1.2);
            }
        }
    }

    private void UpdateTransition(float dt)
    {
        _transitionTimer += dt;
        if (_transitionTimer > 3f)
        {
            _currentChapter = 2;
            _currentMessageIndex = 0;
            _currentMode = GameMode.Novel;
            SwitchExpressionByDialogue(0);
        }
    }

    private void UpdateEvolution()
    {
        if (_score < 1000) _currentThemeColor = Color.Cyan;
        else if (_score < 2000) _currentThemeColor = Color.Magenta;
        else _currentThemeColor = Color.Gold;

        int milestone = _score / 500;
        if (milestone > _lastMilestone)
        {
            _lastMilestone = milestone;
            string[] rawHints = {
                "「想像力がコードを書く……」", "「0と1の間に、魂が宿る。」",
                "「鍵は4番目の次元に隠されている。」", "「鼓動こそが、真実へのパスワード。」",
                "「世界の境界線が溶けていく……。」", "「あなたは、ただの観測者ではない。」",
                "「深淵を覗くとき、深淵もまた……」"
            };
            string hint = rawHints[_random.Next(rawHints.Length)];
            _epiphanies.Add(new EpiphanyText { 
                Text = hint, Position = new Vector2(100 + _random.Next(1000), 500), Timer = 0f, MaxTime = 4f 
            });
        }
    }

    private void TriggerHitEffect(Note note)
    {
        // カメラシェイク
        _cameraShake = new Vector2((float)(_random.NextDouble() - 0.5) * 25f, (float)(_random.NextDouble() - 0.5) * 25f);
        _cameraRotation = (float)(_random.NextDouble() - 0.5) * 0.04f;
        _hitFeedbackTimer = 0.1f;

        // パーティクル生成
        Vector2 pos = new Vector2(_laneX[note.Lane], JudgmentLineY);

        for (int i = 0; i < 12; i++)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float speed = (float)(_random.NextDouble() * 300 + 100);
            _particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed,
                Timer = 0f,
                MaxTime = (float)(_random.NextDouble() * 0.4 + 0.2),
                Color = _currentThemeColor,
                Scale = (float)(_random.NextDouble() * 0.6 + 0.4)
            });
        }

        if (_score > 2500)
        {
            _isGlitching = true;
            _glitchTimer = 0.06f;
        }
    }

    private Vector2 GetProjectedPosition(int lane, float y, float xOffset = 0f)
    {
        float vanishingPointY = 100f;
        float progress = (y - vanishingPointY) / (720f - vanishingPointY);
        progress = MathHelper.Clamp(progress, 0f, 2f);
        
        float x_center = 640f;
        float x_offset_at_bottom = _laneX[lane] - x_center + xOffset;
        
        return new Vector2(x_center + x_offset_at_bottom * progress, y);
    }

    private float GetProjectedScale(float y)
    {
        float vanishingPointY = 100f;
        float progress = (y - vanishingPointY) / (720f - vanishingPointY);
        return MathHelper.Clamp(progress * 2.0f, 0.2f, 2.5f);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        Vector2 screenCenter = new Vector2(1280 / 2, 720 / 2);
        Matrix transform = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *
                           Matrix.CreateRotationZ(_cameraRotation) *
                           Matrix.CreateScale(1.0f + _bgPulse) *
                           Matrix.CreateTranslation(screenCenter.X + _cameraShake.X, screenCenter.Y + _cameraShake.Y, 0);

        if (_isGlitching) transform *= Matrix.CreateTranslation(_random.Next(-10, 11), _random.Next(-10, 11), 0);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transform);

        if (_backgroundImage != null)
        {
            Rectangle bgRect = new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            Color bgColor = (_currentMode == GameMode.RhythmGame || _currentMode == GameMode.Transition) ? Color.Gray : Color.White;
            if (_isGlitching && _score > 2000) bgColor = Color.Lerp(bgColor, Color.DarkSlateBlue, 0.5f);
            
            // 遷移中のフェードアウト演出
            if (_currentMode == GameMode.Transition) bgColor *= (1.0f - (_transitionTimer / 3f));
            
            _spriteBatch.Draw(_backgroundImage, bgRect, bgColor);
        }

        if (_currentCharacterImage != null)
        {
            float scale = 1.0f + _charBobbing;
            if (_score > 2500 && _currentMode == GameMode.RhythmGame) scale += (float)Math.Sin(_totalGameTime * 10f) * 0.02f;
            
            Vector2 origin = new Vector2(_currentCharacterImage.Width / 2, _currentCharacterImage.Height);
            Vector2 position = new Vector2(1280 / 2 + 150, 720);
            
            Color charColor = Color.White;
            if (_score > 2500 && _currentMode == GameMode.RhythmGame) charColor = Color.Lerp(Color.White, _currentThemeColor, 0.3f);
            if (_currentMode == GameMode.Transition) charColor *= (1.0f - (_transitionTimer / 3f));

            _spriteBatch.Draw(_currentCharacterImage, position, null, charColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        switch (_currentMode)
        {
            case GameMode.Novel: DrawNovel(); break;
            case GameMode.RhythmGame: DrawRhythm(); break;
            case GameMode.Transition: DrawTransition(); break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawNovel()
    {
        Rectangle windowRect = new Rectangle(40, 480, 1200, 200);
        _spriteBatch.Draw(_messageWindowTexture, windowRect, Color.White);

        DynamicSpriteFont font = _fontSystem.GetFont(32);
        string[] messages = _chapterMessages[_currentChapter];
        _spriteBatch.DrawString(font, messages[_currentMessageIndex], new Vector2(windowRect.X + 40, windowRect.Y + 40), Color.White);
    }

    private void DrawRhythm()
    {
        DynamicSpriteFont font = _fontSystem.GetFont(32);
        DynamicSpriteFont hintFont = _fontSystem.GetFont(24);
        DynamicSpriteFont keyFont = _fontSystem.GetFont(28);
        DynamicSpriteFont judgeFont = _fontSystem.GetFont(40);

        Color theme = _isHyperMode ? Color.Lerp(_currentThemeColor, Color.Yellow, 0.5f) : _currentThemeColor;

        // サイバーロード・レーン描画 (3D)
        for (int i = 0; i < 4; i++)
        {
            Vector2 pTop = GetProjectedPosition(i, 100);
            Vector2 pBottom = GetProjectedPosition(i, 720);
            Color laneColor = Color.Lerp(_laneColors[i], Color.White, _laneFlash[i]);
            DrawPerspectiveLane(i, laneColor * (0.3f + _laneGlow[i]));
            
            // ヒット時のレーンフラッシュ (足元)
            if (_laneFlash[i] > 0)
            {
                Vector2 flashPos = GetProjectedPosition(i, JudgmentLineY);
                float flashScale = GetProjectedScale(JudgmentLineY);
                Rectangle flashRect = new Rectangle((int)(flashPos.X - 45 * flashScale), (int)(flashPos.Y - 10), (int)(90 * flashScale), 20);
                _spriteBatch.Draw(_messageWindowTexture, flashRect, _laneColors[i] * _laneFlash[i] * 0.5f);
            }
        }

        // 判定ライン描画
        Vector2 pLeft = GetProjectedPosition(0, JudgmentLineY);
        Vector2 pRight = GetProjectedPosition(3, JudgmentLineY);
        Rectangle lineRect = new Rectangle((int)pLeft.X - 50, (int)pLeft.Y - 2, (int)(pRight.X - pLeft.X) + 100, 4);
        _spriteBatch.Draw(_messageWindowTexture, lineRect, theme * 0.6f);

        // ショックウェーブ
        foreach (var s in _shockwaves)
        {
            float alpha = 1.0f - (s.Timer / s.MaxTime);
            _spriteBatch.Draw(_noteTexture, s.Position, null, theme * alpha, 0f, new Vector2(16, 16), s.Scale * 10f, SpriteEffects.None, 0f);
        }

        // エピファニー
        foreach (var epi in _epiphanies)
        {
            float alpha = MathHelper.Clamp(1.0f - (epi.Timer / epi.MaxTime), 0f, 1f);
            Vector2 pos = epi.Position + new Vector2(epi.GlitchOffset, 0);
            _spriteBatch.DrawString(hintFont, epi.Text, pos, theme * alpha);
        }

        // パーティクル
        foreach (var p in _particles)
        {
            float alpha = 1.0f - (p.Timer / p.MaxTime);
            _spriteBatch.Draw(_noteTexture, p.Position, null, p.Color * alpha, 0f, new Vector2(16, 16), p.Scale * 0.4f, SpriteEffects.None, 0f);
        }

        // ノーツ
        foreach (var note in _notes)
        {
            Vector2 pos = GetProjectedPosition(note.Lane, note.Y);
            // ノーツの基本サイズを1.5倍にする
            float scale = GetProjectedScale(note.Y) * 1.5f;
            float rotation = _totalGameTime * 5f + (note.Lane * 0.5f);
            
            Color noteColor = _laneColors[note.Lane];
            float dist = Math.Abs(note.Y - JudgmentLineY);
            if (dist < 30f) {
                noteColor = Color.White;
                // ヒット圏内でのグロー効果
                float glowAlpha = 1.0f - (dist / 30f);
                _spriteBatch.Draw(_noteTexture, pos, null, Color.White * glowAlpha * 0.5f, rotation, new Vector2(16, 16), scale * 1.5f, SpriteEffects.None, 0f);
            }
            else if (dist < 100f) {
                // ニアミス圏内での色変化
                noteColor = Color.Lerp(Color.White, _laneColors[note.Lane], dist / 100f);
            }
            
            _spriteBatch.Draw(_noteTexture, pos, null, noteColor, rotation, new Vector2(16, 16), scale, SpriteEffects.None, 0f);
        }

        // 固定キーガイド (判定棒の下)
        string[] guides = { "D", "F", "J", "K" };
        for (int i = 0; i < 4; i++)
        {
            Vector2 basePos = GetProjectedPosition(i, JudgmentLineY + 50); // 少し下にずらす
            float keyScale = 1.3f - _laneFlash[i] * 0.2f; // テキストサイズ自体も少し大きく
            string keyText = guides[i];
            Vector2 textSize = keyFont.MeasureString(keyText);
            
            Color laneGuiColor = _laneColors[i];
            
            // 外側の背景ボックス（レーン色）
            Rectangle box = new Rectangle((int)(basePos.X - 32 * keyScale), (int)(basePos.Y - 32 * keyScale), (int)(64 * keyScale), (int)(64 * keyScale));
            _spriteBatch.Draw(_messageWindowTexture, box, laneGuiColor * (0.7f + _laneFlash[i] * 0.3f));
            
            // 内側を少し暗くして枠線っぽく見せる
            Rectangle innerBox = new Rectangle((int)(basePos.X - 28 * keyScale), (int)(basePos.Y - 28 * keyScale), (int)(56 * keyScale), (int)(56 * keyScale));
            _spriteBatch.Draw(_messageWindowTexture, innerBox, Color.Black * 0.6f);
            
            _spriteBatch.DrawString(keyFont, keyText, basePos - (textSize / 2) * keyScale, Color.White * (0.9f + _laneFlash[i] * 0.1f), 0f, Vector2.Zero, new Vector2(keyScale, keyScale));
        }

        // 判定文字 (影付きで中央に表示)
        foreach (var jt in _judgeTexts)
        {
            float progress = jt.Timer / jt.MaxTime;
            float alpha = MathHelper.Clamp(1.0f - progress, 0f, 1f);
            float scale = jt.Scale * (1.0f + (float)Math.Sin(progress * Math.PI) * 0.2f);
            Vector2 size = judgeFont.MeasureString(jt.Text);
            Vector2 origin = size / 2;

            // 影
            _spriteBatch.DrawString(judgeFont, jt.Text, jt.Position + new Vector2(3, 3), Color.Black * alpha * 0.5f, 0f, origin, new Vector2(scale, scale));
            // 本文
            _spriteBatch.DrawString(judgeFont, jt.Text, jt.Position, jt.Color * alpha, 0f, origin, new Vector2(scale, scale));
        }

        DrawHUD(font);
    }

    private void DrawPerspectiveLane(int lane, Color color)
    {
        float laneWidthBottom = 90f; // レーンのもっとも手前での幅目安
        
        // レーンの面（塗りつぶし用、簡易版として何本かの水平線を描く）
        for (float y = 100f; y < 720f; y += 4f)
        {
            float progress = (y - 100f) / 620f;
            float widthAtY = laneWidthBottom * progress;
            Vector2 center = GetProjectedPosition(lane, y);
            Rectangle rect = new Rectangle((int)(center.X - widthAtY/2), (int)y, (int)widthAtY, 4);
            _spriteBatch.Draw(_messageWindowTexture, rect, color * 0.15f);
        }

        // 左右の境界線
        Vector2 pLeftTop = GetProjectedPosition(lane, 100f, -laneWidthBottom/2);
        Vector2 pLeftBottom = GetProjectedPosition(lane, 720f, -laneWidthBottom/2);
        Vector2 pRightTop = GetProjectedPosition(lane, 100f, laneWidthBottom/2);
        Vector2 pRightBottom = GetProjectedPosition(lane, 720f, laneWidthBottom/2);

        DrawThickLine(pLeftTop, pLeftBottom, color * 0.6f, 3f);
        DrawThickLine(pRightTop, pRightBottom, color * 0.6f, 3f);

        // スクロールする横線（グリッドラインで奥行きのスピード感を演出）
        float maxZ = 1000f;
        float scrollSpeed = 400f;
        float baseZ = (_totalGameTime * scrollSpeed) % 200f;
        
        for (float z = 0; z <= maxZ; z += 200f)
        {
            float currentZ = (maxZ - z) + baseZ; // 手前に向かってくるZ
            if (currentZ > maxZ) currentZ -= maxZ;

            // Zをパースペクティブ用のprogressへ変換。奥(0)〜手前(1)
            // Zが小さいほど奥(y=100)になるようにする
            float progressZ = currentZ / maxZ; 
            
            // 少し非線形にして、奥から手前に向かうスピード感を出す
            float nonlinearProgress = progressZ * progressZ;

            float lineY = 100f + 620f * nonlinearProgress;
            if (lineY > 100f && lineY < 720f)
            {
                float w = laneWidthBottom * nonlinearProgress;
                Vector2 c = GetProjectedPosition(lane, lineY);
                Rectangle hRect = new Rectangle((int)(c.X - w/2), (int)lineY, (int)w, 2);
                _spriteBatch.Draw(_messageWindowTexture, hRect, color * 0.4f);
            }
        }
    }

    private void DrawThickLine(Vector2 p1, Vector2 p2, Color color, float thickness)
    {
        float angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        float dist = Vector2.Distance(p1, p2);
        _spriteBatch.Draw(_messageWindowTexture, p1, new Rectangle(0,0,1,1), color, angle, new Vector2(0, 0.5f), new Vector2(dist, thickness), SpriteEffects.None, 0f);
    }

    private void DrawHUD(DynamicSpriteFont font)
    {
        Color theme = _isHyperMode ? Color.Yellow : _currentThemeColor;
        _spriteBatch.DrawString(font, $"SCORE: {_score} / {TargetScore}", new Vector2(50, 50), theme);
        
        // エネルギーバー
        Rectangle energyBg = new Rectangle(50, 150, 20, 200);
        _spriteBatch.Draw(_messageWindowTexture, energyBg, Color.White * 0.2f);
        Rectangle energyFill = new Rectangle(50, 150 + (int)(200 * (1.0f - _energy)), 20, (int)(200 * _energy));
        _spriteBatch.Draw(_messageWindowTexture, energyFill, theme);
        
        if (_combo >= 2)
        {
            float comboProgress = (_totalGameTime * 8f) % (float)(Math.PI * 2);
            float comboScale = 1.5f + (float)Math.Sin(comboProgress) * 0.1f;
            string comboText = $"{_combo}";
            string labelText = "COMBO";
            
            if (_isHyperMode) labelText = "HYPER " + labelText;
            
            Vector2 comboSize = font.MeasureString(comboText);
            Vector2 labelSize = font.MeasureString(labelText);
            
            // 中央やや上に表示
            Vector2 centerPos = new Vector2(1280 / 2, 200);
            
            // ラベル (COMBO)
            Vector2 labelPos = centerPos - new Vector2(labelSize.X * 0.4f, 50);
            _spriteBatch.DrawString(font, labelText, labelPos, Color.White * 0.8f, 0f, Vector2.Zero, new Vector2(0.8f, 0.8f));
            // 数字
            Vector2 numPos = centerPos - (comboSize / 2) * comboScale;
            _spriteBatch.DrawString(font, comboText, numPos, Color.Gold, 0f, Vector2.Zero, new Vector2(comboScale, comboScale));
        }

        string stateText = _isHyperMode ? "HYPER IMAGINATION ACTIVE!" : "IMAGINATION SYNCING...";
        _spriteBatch.DrawString(font, stateText, new Vector2(50, 100), theme * 0.7f);
    }

    private void DrawTransition()
    {
        DynamicSpriteFont font = _fontSystem.GetFont(48);
        string text = "SYNCHRONIZED!";
        Vector2 size = font.MeasureString(text);
        Vector2 pos = new Vector2(1280 / 2 - size.X / 2, 720 / 2 - size.Y / 2);
        
        float alpha = Math.Min(1.0f, _transitionTimer);
        if (_transitionTimer > 2f) alpha = 3.0f - _transitionTimer;

        _spriteBatch.DrawString(font, text, pos, Color.Gold * alpha);
    }
}



