/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Assertions;

using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RSG;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/**
   ![Minibuffer in the inspector](inspector/minibuffer.png)
   %Minibuffer is where all the **magic** happens.

   ## Singleton

   %Minibuffer is a singleton that can be accessed with the static `instance`
   property. However, because %Minibuffer may not be instantiated before other
   scripts are run, it is advised that you use the static methods With() or
   Register() when calling %Minibuffer from \link MonoBehaviour.Awake()
   Awake()\endlink, \link MonoBehaviour.Start() Start()\endlink, or \link
   MonoBehaviour.OnDestroy() OnDestroy()\endlink within your own classes.

   ## %Command and %Prompt Attributes

   You can access much of %Minibuffer's functionality by marking up your code
   using the Command, Variable, and Prompt attributes. See each attribute for
   more details.  Here is a typical use case:

   ```
   [Command("hello", description = "Say hello with a custom prompt",
            keyBinding = "C-x h")]
   public string Hello([Prompt("Name: ")] string str) {
     return "Hello, " + str + "!";
   }
   ```

   ## Registering

   If a class has any %Minibuffer attributes, it has to Register() with
   %Minibuffer for those commands or variables to become available.

   ```
   void Start() {
     Minibuffer.Register(this);
   }
   ```

   If a static class has %Minibuffer attributes, register the type.

   ```
   Minibuffer.Register(typeof(ClassX));
   ```

   ### Unregistering

   The behavior for commands and variables that are called after the registering
   object dies is undefined. It is advised to Unregister() when a GameObject is
   destroyed like so:

   ```
   void OnDestroy() {
     Minibuffer.Unregister(this);
   }
   ```

   ## Read Primitives

   There are cases where a command and the Prompt attribute may not be
   adequate. For instance, one may want to ask the user different questions
   depending on the answer to one prompt, or one may only want to prompt the
   user the first time the command is run, not every time. In such cases, the
   Read() method may be of interest.

   ### Read a string

   The Read() method returns a promise of a string. Here is an example taken
   from UnityCommands. It asks the user for the screenshot directory, but only
   the first time the command is run.

   ```
   [Command("capture-screenshot",
            description = "Save screenshot to selected directory",
            keyBinding = "C-c c")]
   public IEnumerator CaptureScreenshot() {
     if (screenshotDirectory == null) {
       // No directory set. Ask for one the first time.
       Minibuffer.instance
         .Read(new Prompt("Screenshot directory: ") {
                          input = "~",
                          history = "screenshot-directory",
                          completer = "directory" }))
         .Then((string path) => {
             screenshotDirectory = path;
             // Then capture the screenshot.
             StartCoroutine(CaptureScreenshot());
           })
         .Catch(ex => { }); // Don't care.
     } else {
       // Capture screenshot
       // ...
     }
   }
   ```

   A couple things to note in the above code: The \link Read(Prompt)
   Read()\endlink method accepts a Prompt object, so all the usage done for
   attributes carries over to this case.

   Also Read() returns a IPromise. It is Then-able and a typical usage will have
   a Then clause and a Catch clause for exceptions. See the
   [C-Sharp-Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise)
   library if your needs go beyond that.

   ### Read an object

   There is a generic method Read<T>() that returns an object of a particular
   type.

   ```
   Minibuffer.instance
     .Read<int>(new Prompt("How much gold do you want: "))
     .Then(number => {
       gold += number;
       Minibuffer.instance.Message("Gold " + gold);
     });
   ```

   The non-generic method calls Read<string>() in its implementation.

   ### Read a char

   You can read a `char` using ReadChar(). (It does not do Read<char>() in its
   implementation.)

   You can read a valid `char` using ReadValidChar(List<char> valid, string
   msg). Repeatedly asks the user for a valid option until they provide one or
   quit with <kbd>C-g</kbd>.

   ### Read a boolean

   There are two convenience methods to read a boolean: ReadYesOrNo(string) and
   ReadTrueOrFalse(string).

   ### Read a %KeyChord

   A KeyChord captures what key was pressed along with any modifiers. There are
   a couple of variations on how to read a KeyChord.

   1. ReadKeyChord() reads the key chord and consumes it; %Minibuffer does not
   handle it afterward.

   2. PeekKeyChord() reads the key chord and does not consume it; %Minibuffer
   handles it afterward.

   3. ReadKeyChord(Func<KeyChord, bool> f) reads the key chord and decides
   whether to consume the input or not.

   ## Dynamic Commands

   Sometimes it is necessary to create a command at run-time rather than
   compile-time. The MiniToggler component, which toggles a GameObject's
   activation, does not know the name of the command or variable at
   compile-time.  It might register a command like this:

   ```
   Minibuffer.instance.RegisterCommand(new Command("toggle-light",
                                                   description = "Turn light on or off"),
                                       () => light.SetActive(! light.activeSelf));
   ```

   There are also generic variations like RegisterCommand<T>() whose Action<T>
   will accept arguments.

   ## Dynamic Variables

   Variables may also be registered dynamically with a get and set pair using
   RegisterVariable<T>().

   ```
   Minibuffer.instance.RegisterVariable<bool>(new Variable("god-mode"),
                                              () => true,
                                              (bool x) => {
                                                if (! x)
                                                  Minibuffer.instance.Message("Nah.");
                                              });
   ```

   ### Unregistering Dynamic Commands and Variables

   Dynamic commands and variables must unregister themselves individually.

   ```
   void OnDestroy() {
     Minibuffer.With(minibuffer => {
       minibuffer.UnregisterCommand("toggle-light");
       minibuffer.UnregisterVariable("god-mode");
     });
   }
   ```

   It is advised to use Minibuffer.With() instead of Minibuffer.instance in
   \link MonoBehaviour.OnDestroy() OnDestroy()\endlink in case %Minibuffer
   itself has been destroyed earlier.

   \nosubgrouping
 */
[HelpURL("http://seawisphunter.com/minibuffer/api/classseawisphunter_1_1minibuffer_1_1_minibuffer.html")]
[Group("minibuffer", tag = "built-in")]
[SelectionBase]
public class Minibuffer : MonoBehaviour {
  /* Good resource on how to decorate these fields for a better editor.
     https://nevzatarman.com/2014/09/21/unity-editor-scripting-it-is-fun/
  */

  private static Minibuffer _instance;
  /** %Minibuffer is a singleton.

      It should be callable anytime after all the Start() methods have been
      called. Unfortunately, it cannot be guaranteed to be available when other
      GameObjects are calling their Start() methods. To ameliorate that issue,
      please see the few static methods Minibuffer exposes: With(), Register(),
      and Unregister().
   */
  public static Minibuffer instance {
    get {
      if (_instance == null) {
        Debug.Log("No Minibuffer.instance available yet. "
                  + "Consider using Minibuffer.OnStart((minibuffer) => { ... }).");
      }
      return _instance;
    }
  }

  /** After %Minibuffer has started, it will run these actions.

      This could be avoided by having the %Minibuffer script run first, but
      that's a per project configuration I want to avoid.
   */
  private static Queue<Action<Minibuffer>> onStart = new Queue<Action<Minibuffer>>();

  internal Interpreter interpreter;
  internal Interpreter.Applied lastApplied;
  internal Interpreter.Applied lastRepeatable;
  internal Dictionary<string, CommandInfo> commands;
  //internal Dictionary<string, WeakReference /*<CommandInfo>*/> dynamicCommands;
  //internal Dictionary<string, CommandInfo> dynamicCommands;
  internal Dictionary<string, MethodInfo> constructors;
  internal Dictionary<string, VariableInfo> variables;
  internal Dictionary<string, GroupInfo> groups;
  private HashSet<string> hintShownForCommand = new HashSet<string>();

  internal GroupInfo anonymousGroup;

  public delegate void GroupCreated(GroupInfo group);
  public event GroupCreated groupCreated;

  internal List<IBuffer> buffers;
  public List<ICurrentProvider> currentProviders = new List<ICurrentProvider>();

  [Tooltip("Font size for minibuffer's text.")]
  [Range(1, 100)]
  public int _fontSize = 14;

  // XXX Put this into class so C-t can work on it and the current completer.
  // [Header("Tab completion settings")]
  [Tooltip("Match completions with input as a prefix or substring?")]
  public Matchers _autoCompleteMatcher = Matchers.PrefixMatcher;
  [Variable("auto-complete-matcher",
            description = "How to match for auto completion")]
  public Matchers autoCompleteMatcher {
    get {
      return _autoCompleteMatcher;
    }
    set {
      _autoCompleteMatcher = value;
      Matcher.defaultMatcher = Matcher.MatcherFromEnum(_autoCompleteMatcher);
    }
  }
  [Tooltip("Should match completions be case sensitive?")]
  public bool _caseSensitive = false;
  [Variable("case-sensitive-matching?",
            description = "Is the auto completion matching case sensitive?")]
  public bool caseSensitiveMatching {
    get { return _caseSensitive; }
    set {
      _caseSensitive = value;
      Matcher.defaultMatcher.caseSensitive = _caseSensitive;
    }
  }
  [Variable("show-annotations",
            description = "Show annotations of completions if available.")]
  [Tooltip("Show annotations of completions if available.")]
  public bool showAnnotations = true;
  [Variable("show-groups",
            description = "Show groups in completion.")]
  public bool showGroups = false;

  public Dictionary<string, CompleterEntity> completers  = new Dictionary<string, CompleterEntity>();
  //internal Dictionary<string, CompleterEntity> completers  = new Dictionary<string, CompleterEntity>();
  //internal Completers completers;
  internal Dictionary<string, List<string>> histories = new Dictionary<string, List<string>>();
  internal Dictionary<string, object> instances       = new Dictionary<string, object>();
  internal Queue<KeyChord> keyQueue  = new Queue<KeyChord>();
  // private HashSet<Type> registeredOnce = new HashSet<Type>();
  /*
     If func returns false, process the KeyChord like normal;
     otherwise, do not process KeyChord.

     var wasHandled = func(keyChord);
   */
  private Queue<Func<KeyChord, bool>> getKeys= new Queue<Func<KeyChord, bool>>();
  private Queue<Func<KeyChord, bool>> getKeysOffline= new Queue<Func<KeyChord, bool>>();
  private Queue<MessageInfo> messageQueue= new Queue<MessageInfo>();
  private MessageInfo lastMessage;


  [Serializable]
  public class MessageSettings {
    [Header("Duration of message types in seconds")]
    [Range(0f, 5f)]
    [Tooltip("The minimum number of seconds a message should be shown (if they're coming rapidly).")]
    public float minSecondsPerMessage = 1f; // seconds

    [Range(0f, 5f)]
    [Tooltip("How long in seconds the alert message will be shown if editing?")]
    public float alertMessageDuration = 1f; // seconds

    [Range(0f, 5f)]
    [Tooltip("How long in seconds the inline message will last, e.g. [No match]?")]
      public float inlineMessageDuration = 1f; // seconds
    [Tooltip("Use a buffer instead of message if text has more lines than...")]
    public int useBufferIfMoreLinesThan = 5; // lines
  };

  [System.Serializable]
  public enum KeyBindingHint {
    Off,
    Show,
    ShowOnce
  };

  [System.Serializable]
  public struct AdvancedOptions {
    public int maxAutocompleteLines;
    public KeyBindingHint keyBindingHint;
    public bool unboundKeyMessage;
    public bool logMessages;
    public float initialWindowHeight;
    public float initialWindowWidth;
    public MessageSettings messageSettings;
  }
  public AdvancedOptions advancedOptions = new AdvancedOptions {
    maxAutocompleteLines = 10,
    keyBindingHint = KeyBindingHint.ShowOnce,
    unboundKeyMessage = true,
    logMessages = true,
    initialWindowHeight = 0.5f,
    initialWindowWidth = 1.0f,
  };

  /**
    The prefixes of the keymap.  If 'C-c' is in prefixes, then of
    the active keymaps there exists a 'C-c *' key binding, so we'll
    keep watching for the next key instead of showing a key binding
    not defined error.  */
  internal string[] prefixes;

  /** universal-argument state */
  internal int? currentPrefixArg = null;

  /** Time the last message was shown. */
  private float lastMessageShown;

  /**
    The list of keymaps is fixed, but they keymaps may be turned on or
    off.  This makes it easier to reason about which keymaps have
    precedent.
   */
  internal List<Keymap> keymaps = new List<Keymap>();
  // This is a list of keymap paths.
  [Tooltip("Load keymaps from the given paths?")]
  private bool loadKeymapsFromPaths = false;
  //[SerializeField]

  private List<string> keymapPaths;
  // [Header("PascalCase, camelCase, kebab-case, or as is?")]
  /** ![Command case setting](files/command-case.png)
      Case convention for commands. */
  [Tooltip("Should commands be forced to be PascalCase, camelCase, kebab-case, or left as is?")]
  public ChangeCase.Case commandCase = ChangeCase.Case.KebabCase;
  /** ![Variable case setting](files/variable-case.png)
      Case convention for variables.*/
  [Tooltip("Should variables be forced to be PascalCase, camelCase, kebab-case, or left as is?")]
  public ChangeCase.Case variableCase = ChangeCase.Case.KebabCase;

  /** Key Sequence Notations */
  public enum Notation {
    /** Use Emacs-like key sequence notation, e.g. M-x. */
    [Description("Emacs: M-x")]
    Emacs,
    /** Use standard key sequence notation, e.g. alt-x. */
    [Description("Standard: alt-x")]
    Standard
  };

  [Space(10)]
  // [Header("")]
  /** ![Key sequence notation setting](files/key-sequence-notation.png) Key
      sequence notation for key bindings. */
  [Tooltip("What key sequence notation do you prefer? Standard alt-x or Emacs' M-x?")]
  public Notation keyNotation = Notation.Standard;

  [Serializable]
  public class GUI {
    public Text status;
    // public Text promptText;
    // public TappedInputField minibufferPrompt;
    public InputField autocompleteField;
    /* We only have a few fixed windows. */
    public Window autocomplete;
    public Window main;
    public RawImage assetPreview;
    // New way of doing prompt without any fancy class.
    public GameObject promptRow;
    public Text prompt;
    public InputField input;
    public HighlightLine highlight;
    public Text inlineMessage;
    public RectTransform echoArea;
  };
  // [Variable("persist-history?", description = "Save history on exit.")]
  /* This is a bad candidate for [Variable] runtime manipulation because if you
     set it to true, it will save history, but when the application is run
     again, it won't load the history. */
  public bool persistHistory = true;

  [Tooltip("Don't alter this unless you know what you're doing.")]
  [FormerlySerializedAs("gui")]
  public GUI GUIElements = new GUI();
  internal GUI gui {
    get { return GUIElements; }
  }
  public delegate void ElementSelectionChangeHandler(string line);
  internal ElementSelectionChangeHandler elementSelectionChange;

  private AppendableBuffer _messages;
  public AppendableBuffer messages {
    get {
      if (_messages == null || ! buffers.Contains(_messages)) {
        // Debug.Log("Making new messages buffer.");
        _messages = new RLEBuffer("*Messages*") { autoScrollDown = true };
        buffers.Add(_messages);
      }
      return _messages;
    }
  }
  internal Window[] windows;
  public const float BORDER_SIZE = 7;
  public const float SCROLLBAR_SIZE = 17.4f;

  internal KeyChord currentKeyChord;
  [NonSerialized]
  public string lastKeySequence;

  private List<string> keyAccum = new List<string>();

  /** %Minibuffer edit state */
  internal struct EditState {
    internal PromptInfo prompt;

    internal List<string> history;
    internal Promise<PromptResult> promise;
    internal Queue<string> prevHistory;
    internal Queue<string> nextHistory;
    internal List<string> candidates; // Match candidates. Null
                                      // entries don't complete.
    internal Action<string> replaceInput;
    internal Func<string, bool> isValidInput;

    internal Func<string, bool> toggleGetter;
    internal Action<string, bool> toggleSetter;

    public void SetupHistory(List<string> newHistory) {
      this.history = newHistory;
      if (this.history != null) {
        this.prevHistory = history.Reverse<string>().ToQueue<string>();
      } else {
        this.prevHistory = new Queue<string>();
      }
      if (this.nextHistory == null)
        this.nextHistory = new Queue<string>();
      this.nextHistory.Clear();
    }

    public void TeardownHistory(string lastInput) {
      if (lastInput != null)
        this.prevHistory.Enqueue(lastInput);
      if (this.history != null) {
        // Set the original history list to what we stored in our queues.
        this.history.Clear();
        this.history
          .AddRange(this.prevHistory.Concat(this.nextHistory));
        // Get rid of zero length strings except the last one.
        // this.history.RemoveAll(s => s.IsZull());
        // Remove consecutive duplicates.
        for (int i = 0; i < this.history.Count - 1; i++) {
          if (this.history[i].IsZull() || this.history[i] == history[i + 1]) {
            this.history.RemoveAt(i);
            i++;
          }
        }
        // Set this.history to null?
      }
    }
  }

  // This doesn't work because structs.
  //public struct PromptResult : PromptResult<object> { }

  internal EditState editState;

  private PromiseTimer promiseTimer = new PromiseTimer();

  private bool _editing;
  internal bool editing {
    get { return _editing; }
    set {
      if (_editing != value) {
        _editing = value;
        if (_editing && ! visible)
          visible = true;
        GetKeymap("editing").enabled = value;
        GetKeymap("minibuffer").enabled = value;
        //status.transform.parent.gameObject.SetActive(!_editing);
        // XXX The ordering here is important.  The cursor can
        // get skewed if it's messed up.

        // gui.minibufferPrompt.enabled = _editing;
        // gui.promptText.enabled = _editing;

        gui.promptRow.SetActive(_editing);

        gui.status.enabled = !_editing;
        if (_editing) {
          ActivateInput();
          // Editing.DeselectAll(gui.input);
          // gui.input.ActivateInputField();
          // this.DoNextTick(() => Editing.DeselectAll(gui.input));
          this.DoNextTick(() =>
                          gui.input.Deselect());
        }
          // this.DoNextTick(() => gui.minibufferPrompt.ActivateInputFieldInternal());
          // this.DoNextTick(() => gui.input.ActivateInputField());
        if (! _editing && EventSystem.current != null) {
          Selectable s = null; //gui.minibufferPrompt.FindSelectableOnUp();
          EventSystem.current.SetSelectedGameObject(s == null ? null : s.gameObject);
        }
      }
    }
  }

  private bool _showAutocomplete = false;
  internal bool showAutocomplete {
    get { return _showAutocomplete; }
    set {
      if (_showAutocomplete != value && value) {
        // We're transitioning to showing.
        gui.autocomplete.scrollRect.verticalNormalizedPosition = 1f;
        //lastTabCompleteInput = "*nope*";
      }
      _showAutocomplete = value;
      gui.autocomplete.window.SetActive(value);
    }
  }

  internal string popupText {
    get { return gui.autocompleteField.text; }
    set {
      gui.autocomplete.sizeInChars
        = new Vector2(value.MaxLineWidth() + 1,
                      Mathf.Min(advancedOptions.maxAutocompleteLines,
                                value.LineCount()));
      //var size = autocomplete.sizeInChars;

      //autocomplete.content.text

      gui.autocompleteField.text = value;
      gui.autocomplete.ScrollToTop();
      showAutocomplete = true;
    }
  }

  /** The input when the user's editing, e.g., in this minibuffer <samp>Name: Shane
   * [No completer]</samp> the input would be "Shane". */
  /**
     The input from the user at the prompt.
   */
  public string input {
    get {
      return gui.input.text;
    }
    set {
      gui.input.text = value;
      // UpdateMinibufferPrompt(value);
    }
  }

  private string _inlineMessage = "";
  private int _inlineMessageId = 0;
  /** The inline message when the user's editing, e.g., in this minibuffer <samp>Name: Shane
      [No completer]</samp> the input would be " [No completer]". 

      When user is editing, an inline message may be shown to the right.

      By convention brackets are often used to denote the inline
      message. */
  protected string inlineMessage {
    get { return _inlineMessage; }
    set { _inlineMessage = value;
      gui.inlineMessage.text = value;
      if (value != "") {
        var id = ++_inlineMessageId;
        promiseTimer
          .WaitFor(advancedOptions.messageSettings.inlineMessageDuration)
          .Then(() =>
              {
                // Only clear on timeout if this was the last message
                // shown.
                if (_inlineMessageId == id)
                  inlineMessage = "";
              });
      }
    }
  }

  // protected void UpdateMinibufferPrompt(string input = null) {
  //   if (input == null)
  //     input = gui.minibufferPrompt.narrowedText;
  //   gui.minibufferPrompt.text = this.prompt + input + this.inlineMessage;
  //   gui.minibufferPrompt.minimumPosition = this.prompt.Length;
  //   gui.minibufferPrompt.maximumPosition
  //     = gui.minibufferPrompt.text.Length - this.inlineMessage.Length;
  // }

  // private string _prompt = "";
  /** The prompt when the user's editing, e.g., In this <samp>Name: Shane
   * [No completer]</samp> the prompt would be "Name: ". */
  public string prompt {
    get { return gui.prompt.text; }
    set { gui.prompt.text = value; }
  }

  /** The status line that's shown when not editing. */
  public string status {
    get { return gui.status.text; }
    set { gui.status.text = value;
      // If it's "", and it was more than two-lines high before it'll stay two
      // lines high.  The following fixes that.
      if (value.IsZull())
        gui.status.SetLayoutDirty();
    }
  }

  [SerializeField]
  [Header("Show on start?")]
  private bool _visible = false;
  private bool _visibleInternal = false;
  private bool animateVisible = true;
  /** Controls the visibility of the minibuffer. */
  public bool visible {
    get { return _visibleInternal; }
    set { if (animateVisible)
            visibleAnim = value;
          else
            visibleNoAnim = value;
        }
  }
  public bool visibleAnim {
    get { return _visibleInternal; }
    set {
      if (_visibleInternal != value) {
        _visibleInternal = _visible = value;

        // Our default is to animate, which is probably a little wrong.
        // See Show() or Hide() for how to avoid the animation.
        StartCoroutine(SetAnchorAnimated(value, 0.3f));
      }
    }
  }

  /*
    This only differs from 'visible' property in that when it is set, the effect
    happens immediately with no animation.
  */
  public bool visibleNoAnim {
    get { return _visibleInternal; }
    set {
      if (_visibleInternal != value) {
        _visibleInternal = _visible = value;
        // Set immediately.
        SetEchoAreaShown(value ? 1f : 0f);
      }
    }
  }

  private IEnumerator SetAnchorAnimated(bool visible, float duration) {
    float start = Time.unscaledTime;
    float t;
    do {
      t = (Time.unscaledTime - start) / duration;
      SetEchoAreaShown(Mathf.SmoothStep(0f, 1f, visible ? t : 1f - t));
      // SetEchoAreaShown(Mathf.Lerp(0f, 1f, visible ? 1f - t : t));
      yield return null;
    } while (t < 1f);
  }

  /*
    Set the proportion of %Minibuffer's echo area that is visible: 0 is not shown; 1 is
    completely visible.
    */
  private void SetEchoAreaShown(float shownProportion) {
    var rt = gui.echoArea;
    var v = rt.anchoredPosition;
    var height = rt.sizeDelta.y;
    v.y = -height * (1f - shownProportion);
    rt.anchoredPosition = v;
  }

  [Variable("font-size",
            description = "Minibuffer font size")]
  public int fontSize {
    get {
      if (gui != null && gui.status != null)
        return gui.status.fontSize;
      else
        return _fontSize;
    }
    set {
      if (fontSize != value && gui != null && value > 0) {
        gui.status.fontSize = value;
        // gui.promptText.fontSize = value;
        gui.prompt.fontSize = value;
        gui.inlineMessage.fontSize = value;
        gui.input.textComponent.fontSize = value;
        if (windows != null)
          foreach (var window in windows) {
            window.fontSize = value;
          }
        // We reset the minibufferPrompt to correct the Caret.
        // No longer needed since at least Unity 5.6.
        // gui.input.enabled = false;
        // gui.input.enabled = true;
        // gui.autocompleteField.enabled = false;
        // gui.autocompleteField.enabled = true;
        // DoNextTick(() => {
            if (! visible) {
              SetEchoAreaShown(0f);
            }
        // });
        _fontSize = value;
      }
    }
  }

  void OnValidate() {
    // Did the user change the font size in the inspector?
    if (_fontSize != fontSize)
      fontSize = _fontSize;
    if (_visibleInternal != _visible)
      visibleNoAnim = _visible;
  }

  public float echoAreaHeight {
    get {
      return gui.echoArea.sizeDelta.y;
    }
  }

  public struct MessageInfo {
    public string message;
    public bool interrupt;
    public bool showInline;
    public MessageInfo(string message,
                       bool interrupt = false,
                       bool showInline = false) {
      this.message = message;
      this.interrupt = interrupt;
      this.showInline = showInline;
    }
  }

  public MinibufferListing minibufferBuiltins;

  private void Awake() {
    if (_instance == null) {
      _instance = this;
    } else {
      Debug.LogWarning("Another Minibuffer object instantiated; killing: there can only be one.", this);
      DestroyImmediate(this.gameObject);
      return;
    }
    commands = new Dictionary<string, CommandInfo>();
    variables = new Dictionary<string, VariableInfo>();
    groups = new Dictionary<string, GroupInfo>();

    anonymousGroup = new GroupInfo(new Group("no-group"));
    groups[anonymousGroup.name] = anonymousGroup;
    KeyChord.standardNotation = (keyNotation == Notation.Standard);
    Register(this);


    //SetupKeymaps();

    buffers = new List<IBuffer>();

    _messages = new RLEBuffer("*Messages*") { autoScrollDown = true };
    buffers.Add(_messages);

    lastMessageShown = Time.unscaledTime;

    windows = new Window[] { gui.autocomplete, gui.main };
    Assert.IsNotNull(gui.autocomplete.window, "autocomplete.window");
    Assert.IsNotNull(gui.main.window, "main.window");

    gui.main.buffer = messages;
    gui.autocomplete.buffer = new Buffer("autocomplete");

    Promise.UnhandledException += Promise_UnhandledException;
    // Setup the current providers.
    currentProviders.Add(new CurrentBuffer());
    currentProviders.Add(new CurrentInputField());
    currentProviders.Add(new CurrentMinibuffer());

    SetupCompleters();

    // Setup the matcher.
    Matcher.defaultMatcher = Matcher.MatcherFromEnum(autoCompleteMatcher);
    Matcher.defaultMatcher.caseSensitive = caseSensitiveMatching;
    if (persistHistory)
      LoadHistory();

    LoadAssembliesWarning.OnWarning(() => {
        if (persistHistory) {
          Debug.Log("Saving history before reload; turning off persistence.");

          SaveHistory();
          persistHistory = false;
        }
      });
    #if UNITY_5_4_OR_NEWER
    SceneManager.sceneLoaded += SceneLoaded;
    #endif
    RegisterType(typeof(ConstructorCommands));
    _visibleInternal = _visible;
    editState.replaceInput = DefaultReplaceInput;
    editState.isValidInput = DefaultValidInput;
    editState.toggleGetter = null;
    editState.toggleSetter = null;
  }

  IEnumerator Start () {
    if (instance != this)
      yield break;

    interpreter = new Interpreter(this);
    CheckGUI();
    fontSize = _fontSize;
    editing = false;

    RunOnStarts();

    yield return null;

    // Make sure to run all of the OnStarts.
    RunOnStarts();
    SetupKeymaps();
    // Let's show our little message if it hasn't been seen already.
    // visibleNoAnim = showOnStart;
    visibleNoAnim = _visible;
    if (! visible) {
      MessageAlert(status); // Let people rewrite this by changing the Status
                            // object.
      //MessageAlert("Welcome to Minibuffer!");
      //MessageAlert("Welcome to Minibuffer! Hit C-h t for tutorial.");
    }
    if (EventSystem.current == null) {
      Debug.LogWarning("No EventSystem available; consider adding one via menu: GameObject->UI->Event System.");
    }
    Minibuffer.Register(typeof(MinibufferVersion));
  }

  void CheckGUI() {
    Assert.IsNotNull(gui.status);
    Assert.IsNotNull(gui.autocompleteField);
    Assert.IsNotNull(gui.autocomplete);
    Assert.IsNotNull(gui.main);
    Assert.IsNotNull(gui.assetPreview);
    Assert.IsNotNull(gui.promptRow);
    Assert.IsNotNull(gui.prompt);
    Assert.IsNotNull(gui.input);
    Assert.IsNotNull(gui.inlineMessage);
    Assert.IsNotNull(gui.echoArea);
    Assert.IsNotNull(gui.highlight);
  }

  void Update () {
    Tick();
    promiseTimer.Update(Time.deltaTime);
  }

  void OnGUI() {
    if (IsInputFieldSelected()) {
      // Don't steal other input field events.
      return;
    }
    KeyEvent(Event.current);
  }

  private bool IsInputFieldSelected() {
    GameObject go;
    if (EventSystem.current != null
        && (go = EventSystem.current.currentSelectedGameObject) != null) {
      // Debug.Log("selected " + go);
      if (go.GetComponent<InputField>() != null
          || go.GetComponent<TappedInputField>() != null) {
        return true;
      }
    }
    return false;
  }

  public void KeyEvent(Event e) {
    var kc = EventToKeyChord(e);
    if (kc != null) {
      // print("got key event " + e);
      Input(kc);
    }
  }

  #if UNITY_EDITOR
  // Doing this in the editor requires some gumption. Ugh.
  private KeyCode lastKeyCode;
  #endif

  public KeyChord EventToKeyChord(Event e) {
    if (e == null || ! e.isKey || KeyChord.IsModifierKey(e.keyCode))
      return null;

    #if UNITY_EDITOR
    if (e.keyCode != KeyCode.None)
      lastKeyCode = e.keyCode;

    // These are stupid special cases required in the Unity Editor.
    if (e.character == 8719)
      return KeyChord.FromString("M-P");
    if (e.character == 732)
      return KeyChord.FromString("M-N");

    bool ctrl = (e.modifiers & EventModifiers.Control) != 0;
    bool fn   = (e.modifiers & EventModifiers.FunctionKey) != 0;
    bool alt  = (e.modifiers & EventModifiers.Alt) != 0;
    bool cmd  = (e.modifiers & EventModifiers.Command) != 0;
    // How the events work in and out of the editor is a bit messy.
    if (e.type == EventType.KeyDown
        && (e.character != '\0' || ctrl || fn || alt || cmd
            // Not sure the rhyme or reason for why return and escape
            // need to be excluded this way.  But this section of code
            // was difficult to get right for Editor, Player, and WebGL
            // so I'm not inclined to make it more parsimonious.
            || e.keyCode == KeyCode.Return
            || e.keyCode == KeyCode.Escape
            )) {
      var newE = new Event(e);
      if (newE.keyCode == KeyCode.None) {
        if (lastKeyCode == KeyCode.None)
          return null;
        else
          newE.keyCode = lastKeyCode;
      }
      if (newE.character > 500)
        newE.modifiers |= EventModifiers.Alt;
      //print("enqueuing key event " + newE);
      var kc = KeyChord.FromEvent(newE);
      var kcp = kc.Canonical(); // kc'
      //print(string.Format("Got {0} but canonical is {1}", kc, kcp));
      // keyQueue.Enqueue(kcp);
      lastKeyCode = KeyCode.None;
      return kcp;
      // XXX Should I use up the paired KeyUp events?
      //if (editing)
      //  e.Use();
    }
    #else
    // It seems to work more cleanly on the build.
    if (e.type == EventType.KeyDown
        && e.keyCode != KeyCode.None
        #if UNITY_WEBGL
        && ! KeyChord.modifierKeys.Contains(e.keyCode.ToString().ToLower())
        #endif
        ) {
      var newE = new Event(e);
      //print("enqueuing key event " + newE);
      return KeyChord.FromEvent(newE).Canonical();
      // XXX Should I use up the paired KeyUp events?
      //e.Use();
    }
    #endif
    return null;
  }

  public void Input(KeyChord k) {
    if (k != null)
      keyQueue.Enqueue(k.Canonical());
  }

#if UNITY_5_4_OR_NEWER
  void SceneLoaded(Scene scene, LoadSceneMode mode) {
#else
  void OnLevelWasLoaded(int level) {
#endif
    completers
      .Values
      .Select(ce => ce.cache)
      .Where(c => c != null)
      .Each(c => c.ResetCache());

    this.DoNextTick(() => RunOnStarts());
  }

  void OnDisable() {
    if (persistHistory)
      SaveHistory();
  }

  void OnDestroy() {
    if (_instance == this)
      _instance = null;
  }

  /*
     After minibuffer starts, it will call any registered action once
     in the order they were registered.

     \note When the level changes, it will call any registered
     actions too.
   */
  private static void OnStart(Action<Minibuffer> action) {
    onStart.Enqueue(action);
  }

  private void RunOnStarts() {
    while (onStart.Any()) {
      //print("running onStart");
      var action = onStart.Dequeue();
      action(this);
    }
  }

  private bool HasTag(string command, string tag) {
    // I do a look up very soon after this too!
    CommandInfo commandInfo;
    if (command != null
        && commands.TryGetValue(command, out commandInfo)) {
      return commandInfo.tags.Contains(tag);
    }
    return false;
  }

  public bool WouldConsumeKeyChord(KeyChord kc) {
    if (kc == null)
      return false;
    var keySeq = keyAccum.Any()
      ? String.Join(" ", keyAccum.ToArray()) + " " + kc.ToString()
      : kc.ToString();

    var command = Lookup(keySeq) ?? Lookup(kc.ToString());
    if (command != null && HasTag(command, "pass-to-inputfield"))
      return false;
    return command != null || prefixes.Contains(keySeq);
  }

  internal bool RunGetKeys(KeyChord keyChord) {
    // We swap back and forth between a getKey queue like it was a
    // framebuffer.
    {
      var getKeysCopy = getKeys;
      getKeys = getKeysOffline;
      getKeysOffline = getKeysCopy;
    }
    bool handled = false;
    // getKeysOffline is offline with respect to anyone outside tampering with
    // it.  Others push stuff into getKeys.  We read stuff off getKeysOffline.
    while (getKeysOffline.Any()) {
      var getKey = getKeysOffline.Dequeue();
      bool handledThis = getKey(keyChord);
      if (handledThis)
        handled = true;
    }
    return handled;
  }

  /**
    This is %Minibuffer's "update" function.
   */
  protected void Tick() {
    if (keyQueue.Any()) {
      currentKeyChord = keyQueue.Dequeue();

      var handled = RunGetKeys(currentKeyChord);
      if (handled)
        return;

      var key = currentKeyChord.ToString();
      keyAccum.Add(key);
      var keySeq = String.Join(" ", keyAccum.ToArray());
      // Debug.Log("key " + key);
      // Debug.Log("keySeq " + keySeq);
      // Debug.Log("prefixes " + String.Join(" ", prefixes.ToArray()));

      lastKeySequence = keySeq;
      //if (ReadyForNextMessage())
      // Echo(keySeq);
      var command = Lookup(keySeq);
      if (command != null) {
        if (! HasTag(command, "no-echo"))
          Echo(keySeq);
        ExecuteCommand(command);
        keyAccum.Clear();
      } else if (prefixes.Contains(keySeq)) {
        // We have to wait for next key press to see.
        // echo area should show the current key.
        Echo(keySeq);
      } else {
        // We've got default behavior. We could make this data driven.
        // if (visible) {
          /*
            Terminology is important. Are keys bound to commands or commands
            bound to keys?
            */
        if (advancedOptions.unboundKeyMessage)
          if (editing)
            // MessageInline(" [No command bound to {0}.]", keySeq);
            MessageInline(" [{0} not bound to any command.]", keySeq);
          else
            // Message("No command bound to {0}.", keySeq);
            Message("{0} not bound to any command.", keySeq);
        // }
        keyAccum.Clear();
      }
    }
    // Show next message in the status window and log.
    if (messageQueue.Any()
        && (visible
            || (gui.main.buffer == messages && gui.main.visible))) {
      var msg = messageQueue.Peek();
      if (msg.showInline) {
        if (msg.interrupt && editing) {
          inlineMessage = msg.message;
        } else {
          if (! ReadyForNextMessage())
            return;
          Echo(msg.message);
          lastMessage = msg;
          lastMessageShown = Time.unscaledTime;
        }
        messageQueue.Dequeue();
      } else {
        // Not an inline message
        if (! editing) {
          if (ReadyForNextMessage()) {
            bool duplicate = lastMessage.message == msg.message;
            var prevLine = status;
            Echo(msg.message);
            lastMessage = msg;
            // XXX messages not setup?
            // Don't wait forever to show duplicate messages.
            if (! duplicate)
              lastMessageShown = Time.unscaledTime;
            if (msg.interrupt) {
              Promise<KeyChord>.Race(PeekKeyChord(),
                                     promiseTimer
                                     .WaitFor(advancedOptions.messageSettings.alertMessageDuration)
                                     .Then(() => Promise<KeyChord>.Resolved(null)))
                .Then((KeyChord keyChord) => {
                    // Replace the previous status line, if we're still showing
                    // the old one.
                    if (status == msg.message)
                      status = prevLine;
                  });
            }
            messageQueue.Dequeue();
          }
        } else {
          // We are editing. Only show the interrupt messages.
          if (! ReadyForNextMessage())
            return;
          Echo(msg.message);
          lastMessage = msg;
          lastMessageShown = Time.unscaledTime;
          if (msg.interrupt) {
            // XXX Maybe this should instead be a way to show the status
            // and hide the other while not actually turning off editing.
            // Not sure. But this PeekKeyChord() trick doesn't work with the
            // new TappedInputField.
            editing = false;
            // Show the alert until a timeout or a key is pressed.
            Promise<KeyChord>.Race(PeekKeyChord(),
                                   promiseTimer
                                   .WaitFor(advancedOptions.messageSettings.alertMessageDuration)
                                   .Then(() => Promise<KeyChord>.Resolved(null)))
              .Then((KeyChord keyChord) => {
                  editing = true;
                  // Input(keyChord);
                  // if (keyChord != null)
                  //   this.DoAfter(new WaitForSeconds(0.1f), () => Input(keyChord));
                });
          }
          messageQueue.Dequeue();
        }
      }
    }
  }

  private void AddKeyBindings(CommandInfo ci, bool overwriteDups = false) {
    var c = ci;
    var keymap = GetKeymap(c.keymap ?? "user", true);
    foreach(var keyBinding in c.keyBindings) {
      if (! overwriteDups && keymap.ContainsKey(keyBinding)
          && keymap[keyBinding] == ci.name) {
        // Do nothing. If we overwrite, it logs a warning.
      } else
        keymap[keyBinding] = ci.name;
    }
  }

  private void RemoveKeyBindings(CommandInfo c) {
    foreach(var kb in c.keyBindings) {
      var keymap = GetKeymap(c.keymap ?? "user", true);
      keymap.Remove(kb);
    }
  }

  private void AddKeyBindings(Dictionary<string, CommandInfo> commands, bool overwriteDups = false) {
    // If any commands are bound to keys in their declaration, let's
    // add them here.
    commands
      .Where(kv => kv.Value.keyBindings.Any())
      .Each(kv => {
          var c = kv.Value;
          var keymap = GetKeymap(c.keymap ?? "user", true);
          foreach(var keyBinding in c.keyBindings) {
            if (! overwriteDups && keymap.ContainsKey(keyBinding) && keymap[keyBinding] == kv.Key) {
              // Do nothing. If we overwrite, it logs a warning.
            } else
              keymap[keyBinding] = kv.Key;
          }
        });
  }

  private void RemoveKeyBindings(Dictionary<string, CommandInfo> commands) {
    // If any commands are bound to keys in their declaration, let's
    // remove them.
    commands
      .Where(kv => kv.Value.keyBindings.Any())
      .Each(kv => {
          var c = kv.Value;
          var keymap = GetKeymap(c.keymap ?? "user", false);
          if (keymap != null) {
            foreach (var keyBinding in c.keyBindings)
              keymap.Remove(keyBinding);
          }
        });
  }

  void SetupKeymaps() {
    var keymapNames = new [] { "user", "minibuffer", "editing", "core",
                               "window", "buffer", /* "compat",*/
                               #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                               "winos",
                               #endif
                               #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                               "macos",
                               #endif
    };

    if (keymapPaths == null || ! keymapPaths.Any()) {
      //print("keymapPaths empty, reseting.");
      keymapPaths = keymapNames
        .Select(s => string.Format("keymaps/{0}.json", s)).ToList();
    }
    if (loadKeymapsFromPaths)
      keymaps = keymapPaths.Select(name => Keymap.Load(name)).ToList();
    var keymapsReset = new List<string>();

    foreach (var keymapName in keymapNames) {
      Keymap keymap = GetKeymap(keymapName, true);
      //if (! keymap.Any()) {
        keymapsReset.Add(keymapName);
        Keymap.LoadDefaultKeymap(keymapName, keymap);
      //}
    }

    Keymap userKeymap = GetKeymap("user", true);
    if (! userKeymap.Any()) {
      keymapsReset.Add("user");
      //print("user keymap empty, reseting.");
    }
    userKeymap.priority = 500;
    AddKeyBindings(commands);
    var editingKeymap = GetKeymap("editing");
    editingKeymap.enabled = false;

    // XXX If I bind anything to say 'n', then I can never hit 'n'
    // again.  When I'm editing the keymap.  I need a reasonable way
    // to deal with this.
    // Workaround: 1. Use quote mechanics: C-q n

    // 2. Make key sequence regexes editing["[a-zA-Z0-9]"] =
    // "self-insert-command";
    //
    // 3. Do something weird with superceding priority (tried; doesn't work).
    //
    // Resolution: Went with option 2. Use regexes sparingly.

    editingKeymap.priority = 900;
    GetKeymap("minibuffer").enabled = false;
    GetKeymap("minibuffer").priority = 1000;
    // GetKeymap("compat").priority = 90;
    if (keymapsReset.Any()) {
      // print("The following keymaps were empty and reset: "
      //       + keymapsReset.OxfordAnd());
    }

    KeymapChanged();
  }

  private void SetupCompleters() {
    //completers["command"]        = new DictCompleter<MethodInfo>(commands).ToEntity();
    var commandCompleter = new CommandCompleter(commands);
    completers["command"]        = commandCompleter.ToEntity();

    var commandNoBindOnlyCompleter = new CommandCompleter(commands);
    commandNoBindOnlyCompleter.hideTags = new [] { "bind-only" };
    completers["command-no-bind-only"]        = commandNoBindOnlyCompleter.ToEntity();

    completers["variable"]       = new DictCompleter<VariableInfo>(variables)
                                     { stringCoerceable = true }.ToEntity();
    completers["variable-int"]   = new DictCompleter<VariableInfo>(variables)
      { stringCoerceable = true,
        filter = (kv) => kv.Value.VariableType == typeof(int) }.ToEntity();
    completers["directory"]      = new FileCompleter()
                                     { directoriesOnly = true }.ToEntity();
    completers["file"]           = new FileCompleter().ToEntity();
    completers["buffer"]         = new ListCompleter<IBuffer>(buffers).ToEntity();
    completers["IBuffer"]        = completers["buffer"];
    completers["Keymap"]         = new ListCompleter<Keymap>(keymaps).ToEntity();
    // Number completers are added dynamically.
    //completers["int"]        = new NumberCompleter(typeof(int));

    // We configure more for userCommands on the next tick.
    var userCommandCompleter = new CommandCompleter(commands);
    userCommandCompleter.hideTags = new [] { "bind-only" };
    completers["user-command"]   = userCommandCompleter.ToEntity();

    this.DoNextTick(() => {
        groups.Keys.Each(commandCompleter.RegisterGroup);
        groupCreated += groupInfo => commandCompleter.RegisterGroup(groupInfo.name);

        GroupCreated hideBuiltinsFn = groupInfo => userCommandCompleter
                                                     .groupVisible[groupInfo.name] = !groupInfo.tags.Contains("built-in");
        // Now...
        groups.Values.Each(gi => hideBuiltinsFn(gi));
        // and into the future.
        groupCreated += hideBuiltinsFn;
        //userCommandCompleter.RegisterGroups();
      });
  }

  private void Promise_UnhandledException(object sender, ExceptionEventArgs e) {
    // AbortException is a normal occurrence. But maybe there should be a flag
    // to run this in a debug mode.
    var ex = e.Exception;
    if (ex is AbortException || ex is MinibufferInUseException) {
      // We aborted. Ok.
    } else {
      Debug.LogError("An unhandled exception in a promise occured!");
      Debug.LogException(e.Exception);
      Message("Error: " + e.Exception.Message);
    }
  }

  /**
     \name Registering and Setup
     Registering with %Minibuffer
     @{
  */

  /**
    Register an instance's commands and variables with %Minibuffer. Ensures it
    will run when the %Minibuffer instance is available.

    It is equivalent to

    ```
    Minibuffer.With(minibuffer => minibuffer.RegisterObject(o));
    ```
  */
  public static void Register(object o) {
    With(m => m.RegisterObject(o));
  }


  /**
     Register an instance's commands and variables with %Minibuffer. Ensures it
     will run when the %Minibuffer instance is available.

     It is equivalent to

     ```
     Minibuffer.With(minibuffer => minibuffer.RegisterObject(o));
     ```
  */
  public static void Register(Type t) {
    With(m => m.RegisterType(t));
  }
  /**
    Unregister an instance's commands and variables with %Minibuffer. Ensures it
    will run when %Minibuffer instance is available. Also protects against the
    case where %Minibuffer might be destroyed before a Component's cleanup
    happens.
   */
  public static void Unregister(object o) {
    With(m => m.UnregisterObject(o));
  }

  /**
     Run an action with the %Minibuffer instance. Run immediately if the current
     instance is available, or defer until the instance becomes available. This
     is most useful in scripts' Awake() or Start() method since the script
     execution order is not guaranteed and the %Minibuffer instance may not be
     set yet.

     %With() makes setup easier and avoids requiring callers to do one of the
     following:

     1. Set the script execution order to guarantee %Minibuffer.instance is
     available.

     2. Write their own defer code (see below).

     You don't have to do this:

     ```cs
     IEnumerator Start() {
       if (Minibuffer.instance == null)
         yield return null;
       Minibuffer.instance.RegisterObject(this);
     }
     ```

     You could do this:

     ```cs
     void Start() {
       Minibuffer.With(m => m.RegisterObject(this));
     }
     ```

     Or even better this:

     ```cs
     void Start() {
       Minibuffer.Register(this));
     }
     ```

     \note The subject-verb form of `%Minibuffer.With` makes this somewhat
     linguistically awkward. A verb-subject form of `With.Minibuffer` would be
     better, and although it's technically do-able, it would introduce
     confusions of its own.

   */
  public static void With(Action<Minibuffer> action) {
    if (_instance != null)
      action(_instance);
    else
      OnStart(action);
  }

  // @}

  /**
     \name Messages and Output
     Print messages to the echo area in minibuffer.
     @{
   */

  /**
     Print a message to the echo area and log it to `*Messages*` buffer. Not
     shown while editing. Messages will be shown for a minimum time. (This can
     be slow. Messages which are repetitions of the last message, do not reset
     the timer.)
   */
  public void Message(string msgFormat, params object[] args) {
    // Message should be shown unless the minibuffer is being used.
    Message(string.Format(msgFormat, args));
  }

  /**
     Print a message to the echo area and log it to the `*Messages*` buffer;
     show it for a limited time even if the user is editing in the minibuffer.
   */
  public void MessageAlert(string msgFormat, params object[] args) {
    // Message will be shown briefly if the minibuffer is being used.
    MessageAlert(string.Format(msgFormat, args));
  }

  /**
    This displays a message in the minibuffer to the right of the input while
    editing. Does not log to `*Messages*` buffer.

    For example, if the minibuffer showed this in the editing area:
    <samp>%Color c: </samp><kbd>blueish</kbd><samp> [No Match]</samp>

    It would be comprised of the following parts:

    [prompt, input, inlineMessage] = ["Color c: ", "blueish", " [No Match]"]
   */
  public void MessageInline(string msgFormat, params object[] args) {
    MessageInline(string.Format(msgFormat, args));
  }

  /**
     Show text in the echo-area. Does not log to messages. Will not show if
     minibuffer editing is active.
  */
  public void Echo(string formatString, params object[] args) {
    Echo(string.Format(formatString, args));
  }

  /**
     Put some content into a buffer and display it.
  */
  public void ToBuffer(string bufferName, string content, bool? wrapText = null) {
    var b = this.GetBuffer(bufferName, true);
    b.content = content;
    Display(b, wrapText);
  }

  /**
     If the output is too large for a message, show it in a buffer.
  */
  public void MessageOrBuffer(string bufferName, string msg) {
    var lineCount = msg.LineCount();
    var b = this.GetBuffer(bufferName, false);
    var displayed = gui.main.buffer == b && gui.main.visible;
    if (lineCount > advancedOptions.messageSettings.useBufferIfMoreLinesThan
        || (b != null && displayed)) {
      // Put it in a buffer.
      b = b ?? this.GetBuffer(bufferName, true);
      b.content = msg;
      Display(b);
    } else {
      Message(msg);
    }
  }
  //@}
  public void Message(string msg) {
    // Message should be shown unless the minibuffer is being used.
    if (advancedOptions.logMessages)
      print("Messsage: " + msg);
    messages.AppendLine(msg);
    messageQueue.Enqueue(new MessageInfo(msg, false));
  }

  public void MessageAlert(string msg) {
    if (advancedOptions.logMessages)
      print("Messsage Alert: " + msg);
    messages.AppendLine(msg);
    messageQueue.Enqueue(new MessageInfo(msg, true));
  }

  public void MessageInline(string msg) {
    if (advancedOptions.logMessages)
      print("Messsage Inline: " + msg);
    messageQueue.Enqueue(new MessageInfo(msg, true, true));
  }

  public void Echo(string text) {
    // print("Echo: " + text);
    if (visible)
      status = text;
  }

  /**
     \name Read Primitives
     Read from the user using the minibuffer.
     @{
  */

  /**
    Prompt the user for input.

    This is a convenience method for Read<T>(Prompt).
   */
  public IPromise<T> Read<T>(string prompt = null,
                             string input = "",
                             string history = null,
                             string completer = null,
                             bool? requireMatch = null,
                             bool? requireCoerce = null) {
    // if (typeof(T) == typeof(char)) {
    //   return ReadChar(
    // }
    var p = new Prompt(prompt) { input = input,
                                 completer = completer,
                                 history = history,
                                 _requireMatch = requireMatch,
                                 _requireCoerce = requireCoerce };
    return Read<T>(p);
  }

  /**
     Prompt the user for input.

     This is the heart and soul of this whole enterprise in a respect. It
     prompts the user for input of some type T, provides a _prompt_, default
     input, history, completer, completions, whether a match with the completer
     is required, whether a coercion is required.

     Note the type T must have an ICoercer available. Many types are supported
     by default: int, float, Color, Vector2, Vector3, Matrix4x4.

     ```
     Minibuffer.instance.Read<float>(new Prompt("Get float: "))
       .Done(number => Minibuffer.instance.Message("Got " + number));
     ```
  */
  public IPromise<T> Read<T>(Prompt prompt) {
    return Read<T>(new PromptInfo(prompt));
  }

  internal IPromise<T> Read<T>(PromptInfo prompt) {
    // XXX I can probably get rid of this, right?
    if (prompt.prompt == null)
      prompt.prompt = typeof(T).PrettyName() + ": ";
    prompt.desiredType = typeof(T);

    // XXX ExecuteCommand<T> should do this.
    return interpreter.Fill<T>(prompt);
      // .Then(obj => {
      //     Debug.Log("Read resolved");
      //     if (obj != null && typeof(T).IsAssignableFrom(obj.GetType())) {
      //       return (T) obj;
      //     } else {
      //       throw new MinibufferException("Unable to convert '{0}' from its type {1} to "
      //                                     + "desired type {2}."
      //                                     .Formatted(obj != null ? obj.ToString() : "null",
      //                                                obj != null ? obj.GetType().ToString() : "<null>",
      //                                                typeof(T)));
      //     }
      //   });
  }

  /**
     Prompt the user for input. If there's a heart and soul to this project,
     this is it.

     Equivalent to `Read<string>(...)`.
     \see Read<T>()
   */
  public IPromise<string> Read(string prompt,
                               string input = "",
                               string history = null,
                               string completer = null,
                               bool? requireMatch = null,
                               bool? requireCoerce = null) {

    var p = new Prompt(prompt) { input = input,
                                 history = history,
                                 completer = completer,
                                 _requireMatch = requireMatch,
                                 _requireCoerce = requireCoerce };
    return Read<string>(p);
  }

  /**
     Equivalent to `Read<string>(prompt)`.
   */
  public IPromise<string> Read(Prompt prompt) {
    return Read<string>(prompt);
  }

  /**
     Equivalent to `Read<string>(prompt)`.
  */
  internal IPromise<string> Read(PromptInfo prompt) {
    return Read<string>(prompt);
  }

  internal IPromise<PromptResult> MinibufferEdit(PromptInfo prompt) {
    return MinibufferEdit(prompt,
                          GetHistory(prompt.history));
  }

  public void DefaultReplaceInput(string completion) {
    this.input = completion;
    gui.input.MoveTextEnd(false);
    gui.input.Deselect();
  }

  /**
     This is kind of the heart and soul of this whole enterprise.  It
     prompts the user for some input, provides context, history, and
     completion for them.
  */
  private IPromise<PromptResult> MinibufferEdit(PromptInfo prompt,
                                                List<string> history) {
    if (editing) {
      MessageInline(" [Minibuffer already in use!]");
      // The abort exception is used just so it'll be ignored when you hit M-x
      // twice.
      return Promise<PromptResult>
        .Rejected(new MinibufferInUseException("Minibuffer already in use!"));
    }
    // Debug.Log("setting field line to 0");
    gui.autocompleteField.SetLineIndex(0);
    this.prompt = prompt.prompt; // Wow, really bad.
    this.inlineMessage = "";
    this.input = prompt.input ?? "";
    editing = true;
    // editState.replaceInput = DefaultReplaceInput;
    // editState.isValidInput = DefaultValidInput;
    this.DoNextTick(() => this.gui.input.MoveTextEnd(false));
    // this.gui.input.MoveTextEnd(false);
    editState.prompt = prompt;
    editState.SetupHistory(history);

    // XXX this could just be tucked into Prompt.
    if (editState.prompt.completerEntity.coercer != null
        && editState.prompt.desiredType == null) {
      editState.prompt.desiredType
        = editState.prompt.completerEntity.coercer.defaultType;
    }
    //editState.callback = callback;
    editState.promise = new Promise<PromptResult>();
    // UpdateMinibufferPrompt(prompt.input);
    // gui.minibufferPrompt.MoveTextEnd(); // move-end-of-line
    return editState.promise;
  }

  private void ActivateInput() {
    gui.input.ActivateInputField();
    this.DoNextTick(() => {
        gui.input.MoveTextEnd(false);
        gui.input.Deselect();
      });
    // print("set selectionfocusposition");
    // gui.input.selectionFocusPosition = gui.input.selectionAnchorPosition = gui.input.text.Length;
  }

  public void InputExit(bool abort) {
    // Debug.Log("inputexit " + this.gui.input.wasCanceled);
    if (! abort && ! this.gui.input.wasCanceled) {
      if (! TryMinibufferExit()) {
        ActivateInput();
      }
    } else
      MinibufferAbort();
    // if (editing) {
    //   // If we're still in editing mode, we're not finished.
    //   ActivateInput();
    // }
  }

  public void EchoAreaClicked() {
    if (editing && ! gui.input.isFocused) {
      ActivateInput();
    }
  }

  /**
     Read the next key pressed and return whether it has been handled. The Func
     `f` returns true if it's been handled, or false if %Minibuffer should
     process it as usual.
   */
  public void ReadKeyChord(Func<KeyChord, bool> f) {
    getKeys.Enqueue(f);
  }

  /**
     Read the next key pressed; %Minibuffer won't process it further.

     This does not go through the standard Read<T> procedure.  It
     merely asks to be the first to receive the next key.  If you
     decide you actually want %Minibuffer to process it, you can add it
     back to the queue: `keyQueue.AddFirst(keyChord)`.  \see PeekKeyChord
  */
  public IPromise<KeyChord> ReadKeyChord() {
    var promise = new Promise<KeyChord>();
    getKeys.Enqueue(k => { promise.Resolve(k);
                           return true; } );
    return promise;
  }

  /**
     Look at the next key but let %Minibuffer still handle it.
  */
  public IPromise<KeyChord> PeekKeyChord() {
    var promise = new Promise<KeyChord>();
    getKeys.Enqueue(k => { promise.Resolve(k);
                           return false; } );
    return promise;
  }

  /**
     Read the next character that is input unless it's the quit command.
  */
  public IPromise<char> ReadChar() {
    return ReadKeyChord()
      .Then(k => {
          if (! IsKeyboardQuit(k.ToString())) {
            char c;
            if (k.ForceCharacter(out c))
              return c;
            else
              throw new MinibufferException("Unable to convert '{0}' to character."
                                            .Formatted(k.ToString()));
          } else
            throw new AbortException("Keyboard Quit");
        });
  }

  /**
     Show a message and request a yes or no response.
  */
  public IPromise<bool> ReadYesOrNo(string msg) {
    return ReadValidChar(new List<char>(new [] {'y', 'n'}), msg)
      .Then(c => c == 'y');
  }

  /**
     Show a message and request a valid character.  (Note: it is case
     insensitive).
  */
  public IPromise<char> ReadValidChar(IEnumerable<char> valid,
                                      string msg) {
    if (valid.Count() == 0)
      throw new ArgumentException("No valid characters provided.", "valid");
    if (editing)
      throw new MinibufferException("Cannot ReadValidChar while editing.");
    var promise = new Promise<char>();
    var options = valid.Select(c => c.ToString()).OxfordOr();
    //var msg = string.Format(msgFormat, args);
    if (msg.Last() != '\n')
      msg += " ";
    msg += "(" + options + ")";
    Action<char> fn = null;
    fn =
      (char c) =>
      {
        c = char.ToLower(c);
        if (valid.Contains(c)) {
          promise.Resolve(c);
        } else {
          // Alert might be the wrong thing here.
          Echo("Please answer " + options + ". "
                       + msg);
          ReadChar()
            .Then(fn)
            .Catch(ex => promise.Reject(ex));
        }
      };
    visibleNoAnim = true;
    Echo(msg);
    // visible = true;
    ReadChar()
      .Then(fn)
      .Catch(ex => promise.Reject(ex));
    return promise;
  }

  /**
     Show a message and request a true or false response.
  */
  public IPromise<bool> ReadTrueOrFalse(string msg) {
    return ReadValidChar(new List<char>(new [] {'t', 'f'}),
                         msg)
      .Then(c => c == 't');
  }

  /**
     Returns a set of objects.
   */
  public IPromise<T> ReadWithToggles<T>(PromptInfo prompt,
                                        Func<string, bool> toggleGetter,
                                        Action<string, bool> toggleSetter,
                                        Func<string, bool> isValidInput = null) {
    var p = Read<T>(prompt);
    if (isValidInput != null)
      editState.isValidInput = isValidInput;
    editState.toggleGetter = toggleGetter;
    editState.toggleSetter = toggleSetter;
    TabComplete();
    return p;
  }

  public IPromise<IEnumerable<T>> ReadSet<T>(PromptInfo prompt) {
    var dict = new Dictionary<string, bool>();
    ICoercer coercer = null;
    var p = ReadWithToggles<T>
      (prompt,
       name => { bool value;
                 return dict.TryGetValue(name, out value) ? value : false; },
       (name, value) => dict[name] = value,
       _ => true)
      .Then(_ => {
          return (IEnumerable<T>)
          dict
          .Where(kv => kv.Value)
          .Select(kv => kv.Key)
          .Select(name => coercer.Coerce(name, typeof(T)))
          .Cast<T>()
          .ToList();
        });
    coercer = editState.prompt.completerEntity.coercer;
    return p;
  }

  public IPromise<IEnumerable<string>> ReadSet(string prompt, IEnumerable<string> toggles) {
    var dict = new Dictionary<string, bool>();
    foreach(var toggle in toggles)
      dict[toggle] = false;
    return ReadSetFromDict(prompt, dict)
      .Then(dict2 => dict.Where(kv => kv.Value).Select(kv => kv.Key));
  }

  public IPromise<Dictionary<string, bool>> ReadSetFromDict(string prompt, Dictionary<string, bool> toggles) {
    var pi = new PromptInfo(prompt);
    pi.completerEntity = new ListCompleter<string>(toggles.Keys).ToEntity();
    var dict = toggles;
    return ReadWithToggles<string>
      (pi,
       name => { bool value;
                 return dict.TryGetValue(name, out value) ? value : false; },
       (name, value) => dict[name] = value,
       _ => true)
      .Then(_ => {
          return dict;
        });
  }

  public IPromise<T> ReadEnumFlags<T>(string prompt, T enumFlags) where T : struct, IConvertible {
    Type type = typeof(T);
    if (type.IsEnum) {
      var pi = new PromptInfo(prompt);
      var names = Enum.GetNames(type);
      pi.completerEntity = new ListCompleter<string>(names).ToEntity();
      int state = Convert.ToInt32(enumFlags);
      // int state = (int) enumFlags;
      return ReadWithToggles<T>(pi,
                      // get toggle
                                name => {
                                  int value = (int) Enum.Parse(type, name, true);
                                  return (state & value) == value;
                                },
                                (name, on) => {
                                  int value = (int) Enum.Parse(type, name, true);
                                  if (on)
                                    state |= value;
                                  else
                                    state &= ~value;
                                },
                                _ => true)
        // .Then(_ => (T) Convert.ChangeType(state, type));
        .Then(_ => (T) Enum.ToObject(type, state));
    } else {
      throw new MinibufferException("Type " + typeof(T) + " is not an Enum.");
    }
  }

  /**
     When TabCompletion works on one item, the popup goes away. That's a
     problem.
   */
  protected void ToggleCompletions(IEnumerable<string> candidates,
                                   Func<string, bool, bool> func) {
    if (editState.toggleGetter != null
        && editState.toggleSetter != null) {
      foreach (var name in candidates) {
        var state = editState.toggleGetter(name);
        editState.toggleSetter(name,
                                 func(name,
                                      state));
      }
      // Redraw.
      TabComplete();
    } else {
      MessageInline(" [Not toggle-able]");
    }
  }

  protected void ToggleCompletions(IEnumerable<string> candidates,
                                   Func<bool, bool> func) {
    ToggleCompletions(candidates, (_, state) => func(state));
  }

  [Command(keyBinding = "C-1", description = "Turn on toggle.", keymap = "minibuffer")]
  public void ToggleOn() {
    var name = GetElement();
    ToggleCompletions(new [] { name }, _ => true);
  }

  [Command(keyBinding = "C-S-1", description = "Turn on all toggles.", keymap = "minibuffer")]
  public void ToggleOnMany() {
    ToggleCompletions(editState.candidates, _ => true);
  }

  [Command(keyBinding = "C-0", description = "Turn off toggle.", keymap = "minibuffer")]
  public void ToggleOff() {
    var name = GetElement();
    ToggleCompletions(new [] { name }, _ => false);
  }

  [Command(keyBinding = "C-S-0", description = "Turn off all toggles.", keymap = "minibuffer")]
  public void ToggleOffMany() {
    ToggleCompletions(editState.candidates, _ => false);
  }

  [Command(keyBinding = "C-2", description = "Toggle item.", keymap = "minibuffer")]
  public void ToggleInvert() {
    var name = GetElement();
    ToggleCompletions(new [] { name }, value => ! value);
  }

  [Command(keyBinding = "C-S-2", description = "Toggle all items.", keymap = "minibuffer")]
  public void ToggleInvertMany() {
    ToggleCompletions(editState.candidates, value => ! value);
  }

  /**
     Read a set of toggles.
  */
  public IPromise<Dictionary<object, bool>> ReadSet1(Dictionary<object, bool> toggles) {
    var reservedLetters = new [] {'q', '*', '1', '0'};
    var letterBag = new LetterBag();
    foreach(var r in reservedLetters)
      letterBag.chars.Add(r);
    var nameAndVars = new Dictionary<string, object>();
    var charToVar = new Dictionary<char, object>();
    foreach (var obj in toggles.Keys) {
      var name = obj.ToString();
      char c;
      var nameWithShortcut = letterBag.NextLetter(name, out c);
      charToVar[c] = obj;
      nameAndVars[nameWithShortcut] = obj;
    }

    System.Func<char, bool> getChar = (c) => toggles[charToVar[c]];
    Action<char, bool> setChar = (c, b) => toggles[charToVar[c]] = b;
    System.Func<string> currentState = () => {
      var sb = new System.Text.StringBuilder();
      foreach (var kv in nameAndVars) {
        sb.Append("\n");
        if (toggles[kv.Value])
          sb.Append("[X] ");
        else
          sb.Append("[ ] ");
        sb.Append(kv.Key);
      }
      sb.Remove(0, 1); // Remove first newline.
      return sb.ToString();
    };
    var p = new Promise<Dictionary<object, bool>>();
    ReadMaskHelper(charToVar.Keys.ToList(),
                   getChar,
                   setChar,
                   currentState,
                   () => {
                     showAutocomplete = false;
                     p.Resolve(toggles);
                   });
    return p;
  }

  private void ReadMaskHelper(List<char> chars,
                              System.Func<char, bool> getChar,
                              Action<char, bool> setChar,
                              Func<string> currentState,
                              // Dictionary<char, object> charToVar,
                              // Dictionary<object, bool> objToBool,
                              Action onQuit) {
    popupText = currentState();
    // showAutocomplete = true;
    // var sb = new StringBuilder();
    // foreach (var key in objToBool) {
    //   if (objToBool[key])
    //     sb.Append("[X] ");
    //   else
    //     sb.Append("[ ] ");
    //   sb.Append(key.ToString());
    // }
    // popupText = sb.ToString();
    ReadValidChar(chars.Concat(new [] {'q', '1', '0', '*'}), "Toggle (q to quit)")
      .Then(ch => {
          switch(ch) {
            case 'q':
            {
              onQuit();
              return;
            }
            case '1':
            foreach(var c in chars) {
              setChar(c, true);
            }
            break;
            case '0':
            foreach(var c in chars) {
              setChar(c, false);
            }
            break;
            case '*':
            foreach(var c in chars) {
              setChar(c, ! getChar(c));
            }
            break;
            default:
            {
              setChar(ch, ! getChar(ch));
              break;
            }
          }
          ReadMaskHelper(chars, getChar, setChar, currentState, onQuit);
        })
      .Catch(ex => onQuit());
  }
  ///@}

  public IPromise<T> ExecuteCommand<T>(string command) {
    return ExecuteCommand(command)
      .Then(obj => {
            if (obj != null && typeof(T).IsAssignableFrom(obj.GetType())) {
              return (T) obj;
            } else {
              throw new MinibufferException("Unable to convert '{0}' from its type {1} to "
                                            + "desired type {2}."
                                            .Formatted(obj,
                                                       obj != null ? obj.GetType().ToString() : "<null>",
                                                       typeof(T)));
            }
        });
  }

  public IPromise<object> ExecuteCommand(string command) {
    command = CanonizeCommand(command);
    //print("execute command: " + command);
    CommandInfo commandInfo;
    //Delegate commandDelegate;
    if (command != null
        && commands.TryGetValue(command, out commandInfo)) {
      //var methodInfo = commandInfo.methodInfo;
      var methodInfo = commandInfo.delegate_.Method;
      return interpreter.Apply(commandInfo.delegate_,
                               commandInfo.parameterNames,
                               commandInfo.prompts)
        .Then(result => {
            if (! commandInfo.tags.Contains("no-repeat"))
              lastRepeatable = interpreter.lastApplied;
            lastApplied = interpreter.lastApplied;
            currentPrefixArg = null;
            if (methodInfo.ReturnType == typeof(IEnumerator)) {
              #if ! UNITY_WEBGL
              /*
                XXX This code causes an Assertion failure: klass->initalized
                when its run while changing a scene. It looks like it has
                something to do with the Promise being invoked on an action. So
                instead we just immediately return null when a coroutine command
                is run.

                This could probably be avoided somewhat, by restricting the
                promise to coroutine code path only when we're going to destroy
                the scene.

                This bug took a LONG time to find!
               */
              var p = new Promise();
              StartCoroutine(PromisedCoroutine((IEnumerator) result, p));
              return p
                .Then<object>(() => { return null; });
              #else
              StartCoroutine((IEnumerator) result);
              return Promise<object>.Resolved(null);
              #endif
            } else {
              return Promise<object>.Resolved(WithCommandResult(result));
            }
          });
    } else {
      var msg = "No such command: " + command;
      MessageAlert(msg);
      currentPrefixArg = null;
      return Promise<object>.Rejected(new MinibufferException(msg));
    }
  }

  private IEnumerator PromisedCoroutine(IEnumerator coroutine, Promise p) {
    yield return coroutine;
    p.Resolve();
  }

  public object WithCommandResult(object result) {
    if (result != null && result is string)
      Message((string) result);
    return result;
  }

  [Command("execute-extended-command",
           description = "Reads a command and its arguments then executes it.",
           group = "core",
           keyBindings = new [] { "M-x", ":" },
           tags = new [] { "no-repeat", "bind-only" })]
  public void ExecuteExtendedCommand(string completer = "command-no-bind-only") {
    int? passingPrefixArg = currentPrefixArg;
    currentPrefixArg = null;
    var M_x = currentKeyChord.ToString(); // "M-x" usually
    if (M_x != ":") // A vim-ism for you vimsters.
      M_x += " ";
    // XXX If you run M-x execute-extended-command
    // M_x will be "return".  We can guard against that
    // but maybe it's not worth it.
    Read(passingPrefixArg == null
         ? M_x
         : (passingPrefixArg == 4
            ? KeyChord.Canonize("C-u")
            : passingPrefixArg.ToString()) + " " + M_x,
         "", // default input
         "command",
         completer, // completer
         true // require match
         )
      .Done(cmd =>  // when finished
          {
            currentPrefixArg = passingPrefixArg;
            if (cmd != null) {
              var bindings = KeyBindingsForCommand(cmd);
              if (bindings.Any() && advancedOptions.keyBindingHint >= KeyBindingHint.Show) {
                if (! (//advancedOptions.keyBindingHint.HasFlag(KeyBindingHint.ShowOnce)
                       advancedOptions.keyBindingHint == KeyBindingHint.ShowOnce
                       && hintShownForCommand.Contains(cmd))) {
                  MessageAlert("You can run the command {0} with {1}.",
                               cmd,
                               bindings.OxfordOr());
                }
                hintShownForCommand.Add(cmd);
              }
              ExecuteCommand(cmd);
            }
          });
      // .Catch(ex => {
      //     if (ex is AbortException || ex is MinibufferInUseException) {
      //       // We aborted. Ok.
      //     } else {
      //       Debug.LogException(ex);
      //     }
      //   });
  }


  [Command("execute-user-command",
           description = "Executes a command; does not show system commands.",
           group = "core",
           keyBinding = "M-X",
           tags = new [] { "no-repeat", "bind-only" })]
  public void ExecuteUserCommand() {
    ExecuteExtendedCommand("user-command");
  }

  [Command("minibuffer-exit",
           description = "Exit from minibuffer editing. "
           + "If matching is required and input is not a match, then use-element or minibuffer-complete will be called. "
           + "If coercion or matching is required and not fulfilled, an error message will show.",
           keymap = "minibuffer",
           keyBinding = "return",
           tags = new [] { "no-repeat", "bind-only" })]
  /** Return true if minibuffer can exit gracefully, false if minibuffer did not exit. */
  public bool TryMinibufferExit() {
    // print("minibuffer-exit");
    if (editState.isValidInput == null || editState.isValidInput(input)) {
      // print("minibuffer-exit ok");
      MinibufferExit(false);
      return true;
    }
    // Debug.Log("Input is not valid according to editState.isValidInput.");
    // print("minibuffer-exit nope");
    return false;
  }

  internal bool DefaultValidInput(string input) {
    if ((editState.prompt.requireMatch || input.IsZull())
        && ! CompleterHasMatch(input)) {
      if (gui.autocomplete.window.activeInHierarchy) {
        UseElement();
      } else {
        // Don't let it go thru.
        TabComplete(); // Show [No match] or [Invalid format].
      }
      return false;
    } else if (editState.prompt.requireCoerce && ! CanCoerce(input)) {
      MessageInline(" [Cannot coerce to {0}]",
                    editState.prompt.desiredType.PrettyName());
      return false;
    } else {
      return true;
    }
  }

  [Command("minibuffer-abort",
           description = "Aborts from minibuffer editing.",
           tag = "no-repeat")]
  public void MinibufferAbort() {
    MinibufferExit(true);
  }

  /*
    End our minibuffer editing, one way or another.
   */
  internal void MinibufferExit(bool abort) {
    if (! editing) {
      Debug.LogWarning("MinibufferExit called while not 'editing'.");
      return;
    }

    var inputCopy = input;
    if (! abort
        && (editState.isValidInput == null
            || ! editState.isValidInput(inputCopy))) {
        // && ((editState.prompt.requireMatch
        //      && ! CompleterHasMatch(inputCopy))
        //     || (editState.prompt.requireCoerce
        //         && ! CanCoerce(inputCopy)))) {
      // If matches or coercion are required but can't be satisfied, don't let
      // minibuffer exit.
      Debug.Log("Input is not valid according to editState.isValidInput.");
      return;
    }
    showAutocomplete = false;
    editState.TeardownHistory(inputCopy);
    // Get rid of all nulls.
    editState.history.RemoveAll(s => s.IsZull());
    editState.replaceInput = DefaultReplaceInput;
    editState.isValidInput = DefaultValidInput;
    editState.toggleGetter = null;
    editState.toggleSetter = null;

    this.inlineMessage = "";
    // UpdateMinibufferPrompt("");
    Echo(""); // Clear the status window; otherwise it's confusing.
    editing = false;
    gui.assetPreview.enabled = false;
    // if (editState.callback != null) {
    //   editState.callback(abort ? null : inputCopy,
    //                           abort ? null : context); // Should use a sentinal
    //                                                    // value for aborts.
    //   // Setting callback to null breaks everything. :(
    //   //callback = null;
    // }
    var oldPrompt = editState.prompt;
    editState.prompt = null;
    object result = null;
    try {
      var coercer = oldPrompt.completerEntity.coercer;
      if (coercer != null) {
        Assert.IsNotNull(oldPrompt.desiredType);
        result = coercer.Coerce(inputCopy,
                                oldPrompt.desiredType);
        // If we're dealing with a string that we don't _have_ to coerce,
        // then let the string through.  It's a special case.
        if (result == null && ! oldPrompt.requireCoerce && oldPrompt.desiredType == typeof(string))
          result = inputCopy;
      }
    } catch (CoercionException e) {
      if (editState.promise != null) {
        editState.promise.Reject(e);
        return;
      } else {
        throw e;
      }
    } finally {
      //oldPrompt.completerEntity.Clear();
      editState.candidates = null;
    }
    if (editState.promise != null) {
      if (abort) {
        editState.promise
          .Reject(new AbortException("Minibuffer aborted."));
      } else {
        PromptResult pr = new PromptResult();
        pr.str = inputCopy;
        pr.obj = result;
        editState.promise.Resolve(pr);
      }
    }
  }

  /*
    Let's allow ourselves to put the minibuffer away and put it back
    again.  This could allow us to have recursive minibuffers too.
   */
  internal EditState SaveEditState() {
    // Make a copy of the prompt.
    editState.prompt = new PromptInfo(editState.prompt);
    editState.prompt.input = this.input;
    return editState;
  }

  internal void RestoreEditState(EditState state) {
    editState = state;
    this.input = editState.prompt.input;
    this.prompt = editState.prompt.prompt;
  }

  internal void ClearEditState() {

    // editState.TeardownHistory(inputCopy);
    // Get rid of all nulls.
    // editState.history.RemoveAll(s => s.IsZull());
    editState.replaceInput = DefaultReplaceInput;
    editState.isValidInput = DefaultValidInput;
    editState.toggleGetter = null;
    editState.toggleSetter = null;
  }

  internal bool CompleterHasMatch(string input) {
    return (editState.prompt.completerEntity.completer != null
            && editState.prompt.completerEntity.completer.Complete(input)
                 .Where(x => x == input).Any())
            || editState.prompt.completerEntity.completer == null; // A 'null' completer matches everything. (or nothing?)
  }

  internal bool CanCoerce(string input) {
    if (editState.prompt.completerEntity.coercer == null)
      return false;
    try {
      return editState.prompt.completerEntity.coercer
        .Coerce(input, UnwrapType(editState.prompt.desiredType)) != null;
    } catch (CoercionException) {
      return false;
    }
  }

  /*
    Unwrap PromptResult<T> and the like and return typeof(T).
   */
  private Type UnwrapType(Type t) {
    if (t == typeof(PromptResult))
      return typeof(object);
    else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PromptResult<>))
      return t.GetGenericArguments()[0];
    else
      return t;
  }

  [Command("minibuffer-complete",
           description = "Tab complete or show available completions",
           keymap = "minibuffer",
           keyBinding = "tab",
           tags = new [] { "no-repeat", "bind-only" })]
  public void MinibufferComplete() {
    var inputCopy = this.input;
    var matches = TabComplete(inputCopy);

    if (editState.prompt.completerEntity.completer != null) {
      if (matches.Count > 4) {
        // Humans can count to four natively.
        MessageInline(" [{0} matches]", matches.Count);
      }

      if (matches.Count == 1) {
        showAutocomplete = false;
        if (inputCopy == matches[0])
          MessageInline(" [Sole completion]");
        editState.replaceInput(matches[0]);
      } else if (matches.Count == 0) {
        showAutocomplete = false;
        // We should distinguish between matches and coercions.
        if (editState.prompt.requireCoerce && ! CanCoerce(inputCopy))
          MessageInline(" [No match; cannot coerce]");
        else if (editState.prompt.desiredType != typeof(string))
          MessageInline(" [No match; can coerce to {0}]",
                        editState.prompt.desiredType.PrettyName());
        else
          MessageInline(" [No match]");
      } else {
        if (matches.Contains(inputCopy))
          MessageInline(" [Complete but not unique]");
      }

    } else if (editState.prompt.requireCoerce
               && editState.prompt.completerEntity.coercer != null) {
      //var coercer = editState.completerEntity.coercer;
      if (/*editState.requireCoerce && */! CanCoerce(inputCopy))
        MessageInline(" [Cannot coerce to {0}]",
                      editState.prompt.desiredType.PrettyName());
      // Showing an inline message for something that's ok seems like it's over
      // doing it.

      // else if (editState.desiredType != typeof(string))
      // MessageInline(" [Can coerce to {0}]",
      // editState.desiredType.PrettyName());
    } else {
      showAutocomplete = false;
      MessageInline(" [No completer]");
    }
  }

  // public List<string> TabComplete(string inputCopy = null) {
  //   return TabComplete<string>(inputCopy);
  // }

  public List<string> TabComplete(string inputCopy = null) {
    if (inputCopy == null)
      inputCopy = this.input;

    if (editState.prompt.completerEntity.completer != null) {
      ICompleter<string> completer = new CompleterAdapter2(editState.prompt.completerEntity.completer);

      var annotater = editState.prompt.completerEntity.annotater;
      if (showAnnotations && annotater != null) {
        completer = new AnnotaterCompleter<string>(completer, annotater);
      }

      var grouper = editState.prompt.completerEntity.grouper;
      if (showGroups && grouper != null) {
        completer = new GrouperCompleter<string>(completer, grouper);
      }
      if (editState.toggleGetter != null) {
        completer = new ToggleCompleter<string>(completer,
                                                editState.toggleGetter);
      }
      var completions = completer.Complete(inputCopy).ToList();
      var matches = completions.Select(x => completer.MatchCompletion(x)).ToList();
      var lines = completions.Select(x => completer.DisplayCompletion(x)).ToList();
      // ICompleter<T> takes care of the order.
      // matches.Sort();

      if (matches.Count <= 1) {
        return matches;
      } else {
        // There is more than one match.
        var maxLength = lines.Aggregate(0, (accum, str) => Math.Max(accum, str.Length));
        gui.autocompleteField.SetLineIndex(gui.autocompleteField.GetLineIndex()
                                           .Clamp(0,
                                                  matches.Count));

        gui.autocomplete.sizeInChars
          = new Vector2(maxLength + 1,
                        Mathf.Min(advancedOptions.maxAutocompleteLines, completions.Count));
        gui.autocompleteField.text = String.Join("\n", lines.ToArray());
        gui.autocomplete.ScrollToTop();
        showAutocomplete = true;

        // XXX This doesn't work with GroupCompleter.
        editState.candidates = matches;
        var longestMatch = LongestMatch(inputCopy, matches.Where(x => x != null).ToList());
        var caseInsensitive = true;
        if (longestMatch.StartsWith(inputCopy, caseInsensitive, null))
          editState.replaceInput(longestMatch);
        return matches;
      }
    } else if (editState.prompt.requireCoerce
               && editState.prompt.completerEntity.coercer != null) {
      // if (! CanCoerce(inputCopy))
      //   MessageInline(" [Cannot coerce to {0}]",
      //                 editState.prompt.desiredType.PrettyName());
      return null;
    } else {
      // There is no completer.
      return null;
    }
  }

  /*
    Computes the longest matching string, e.g. if prefix is 'c' and strings are
    { 'close', 'clobber' } then this method will return 'clo'.
   */
  private static string LongestMatch(string prefix, List<string> strings) {
    int matchLength = Math.Max(0, prefix.Length);
    if (matchLength != 0) {
      for (;;) {
        if (matchLength >= strings[0].Length)
          break;
        char c = char.ToLower(strings[0][matchLength]);
        for (int i = 1; i < strings.Count; i++) {
          if (matchLength >= strings[i].Length
              || char.ToLower(strings[i][matchLength]) != c)
            goto endOfLoop;
        }
        matchLength++;
      }
    }
  endOfLoop:
    var longestMatch = strings[0].Substring(0, Math.Min(matchLength, strings[0].Length));
    return longestMatch;
  }

  /**
     \name Auto complete popup
     Manipulates the auto complete popup state
     @{
  */

  [Command("next-element",
           description = "Select the next element in autocomplete popup",
           keymap = "minibuffer",
           keyBinding = "C-n",
           tags = new [] { "no-repeat", "bind-only" })]
  public void NextElement() {
    var line = gui.autocompleteField.GetLineIndex();
    if (line < gui.autocompleteField.GetLineCount() - 1) {
      gui.autocompleteField.SetLineIndex(line + 1);
      this.DoNextTick(() => {
          if (! gui.highlight.isLineVisible)
            gui.autocomplete.ScrollDown();
        });
//       // XXX This won't work with annotations.
//       if (! gui.highlight.isLineVisible)
//         gui.autocomplete.ScrollDown();
      //ShowPreviewForCurrentLine();
      // XXX This won't work with annotations.// 
      if (elementSelectionChange != null && editState.candidates != null)
        elementSelectionChange(editState.candidates[gui.autocompleteField.GetLineIndex()]);
    }
  }

  [Command("previous-element",
           description = "Select the previous element in autocomplete popup",
           keymap = "minibuffer",
           keyBinding = "C-p",
           tags = new [] { "no-repeat", "bind-only" })]
  public void PreviousElement() {
    var line = gui.autocompleteField.GetLineIndex();
    if (line > 0) {
      gui.autocompleteField.SetLineIndex(line - 1);
      this.DoNextTick(() => {
          if (! gui.highlight.isLineVisible)
            gui.autocomplete.ScrollUp();
        });
      // XXX This won't work with annotations.
      if (elementSelectionChange != null && editState.candidates != null)
        elementSelectionChange(editState.candidates[gui.autocompleteField.GetLineIndex()]);
      //gui.autocompleteField.GetLineAt(gui.autocompleteField.lineIndex));
      //ShowPreviewForCurrentLine();
    }
  }

  /*
    Returns the current line selected in the popup window.
   */
  public string GetElement() {
    if (gui.autocomplete.window.activeInHierarchy) {
      if (editState.candidates != null
          && gui.autocompleteField.GetLineIndex() < editState.candidates.Count
          && gui.autocompleteField.GetLineIndex() >= 0) {
        var c = editState.candidates[gui.autocompleteField.GetLineIndex()];
        return c;
      }
    }
    return null;
  }

  [Command("use-element",
           description = "Insert the current completion into minibuffer.",
           keymap = "minibuffer",
           keyBinding = "C-return",
           tags = new [] { "no-repeat", "bind-only" })]
  public void UseElement() {
    var c = GetElement();
    if (c != null && editState.replaceInput != null) {
      editState.replaceInput(c);
    }
  }
  ///@}

  [Command("keyboard-quit",
           description = "Quit the current operation; two quits will hide minibuffer.",
           group = "core",
           keyBindings = new [] { "C-g", "escape" },
           tags = new [] { "no-repeat", "bind-only" })]
  public void KeyboardQuit() {
    if (lastApplied.methodInfo != null
        && lastApplied.methodInfo.Name == "KeyboardQuit") {
      windows.Each(w => w.visible = false);
      visible = false;
      // If we're quitting and disappearing everything, we want the quit go to
      // the log not the queue; otherwise, it'll clog up the queue such that any
      // Echo()s that happen will be overwritten.  The test case that provoked this
      // was C-t or M-x toggle-booleans not showing anything in the echo area because
      // "Quit" was in the queue.
      messages.AppendLine("Quit");
    } else {
      Message("Quit");
    }
    if (editing)
      MinibufferAbort();
  }

  private string keyboardQuitCommand = null;
  public bool IsKeyboardQuit(string key) {
    if (keyboardQuitCommand == null)
      keyboardQuitCommand = CanonizeCommand("keyboard-quit");
    return Lookup(key) == keyboardQuitCommand;
  }

  /*
    Have we shown the last message for long enough?
   */
  private bool ReadyForNextMessage() {
    return (advancedOptions.messageSettings.minSecondsPerMessage > 0f
            && (Time.unscaledTime - lastMessageShown) > advancedOptions.messageSettings.minSecondsPerMessage)
      || advancedOptions.messageSettings.minSecondsPerMessage <= 0f;
  }

  /**
    Resolve a key sequence to a command name or null.
  */
  public string Lookup(string keyseq, bool enabledKeymapsOnly = true) {
    // print("Keymap sequence: " + keymaps.Select(k => k.name).OxfordAnd());
    // print("Commands found: " + keymaps
    //       .Where(x => x.enabled && x.map.ContainsKey(keyseq))
    //       .Select(y => y[keyseq]).OxfordAnd());
    return keymaps
      .Where(x => (x.enabled || ! enabledKeymapsOnly) && x.ContainsKey(keyseq))
      .Select(y => y[keyseq])
      .FirstOrDefault();
  }

  /**
    Get or make a new completer for a type.
   */
  // public ICompleter GetCompleter(Type type, bool createIfPossible = true) {
  //   return GetCompleterEntity(type, createIfPossible).completer;
  // }

  public CompleterEntity GetCompleterEntity(Type type,
                                            bool createIfPossible = true) {
    CompleterEntity completerEntity;
    if (! completers.TryGetValue(type.PrettyName(), out completerEntity)
        && createIfPossible) {
      // No completer, try to make one.
      if (type.IsSubclassOf(typeof(UnityEngine.Behaviour))) {
        //print("making component completer " + type);
        completerEntity = new BehaviourCompleter(type).ToEntity();
        completers[type.PrettyName()] = completerEntity;
      } else if (type.IsSubclassOf(typeof(UnityEngine.Object))) {
        // We can find anything that's a UnityEngine.Object.
        completerEntity = new ResourceCompleter(type).ToEntity();
        completers[type.PrettyName()] = completerEntity;
      } else if (type.IsEnum) {
        completerEntity = new EnumCompleter(type).ToEntity();
        completers[type.PrettyName()] = completerEntity;
      } else if (type.IsNumericType()) {
        // If it's a number, we can do it.
        completerEntity = new CompleterEntity(new NumberCoercer(type));
        completers[type.PrettyName()] = completerEntity;
      }
    }
    return completerEntity;
  }

  /*
    Get the prefixes of the given key bindings, e.g.\ these three
    keybindings C-x 1, C-x 2, C-f, C-h C-h have the prefixes C-x and
    C-h.
   */
  public static IEnumerable<string> GetPrefixes(IEnumerable<string>
                                                keyBindings) {
    return keyBindings
      .Where(x => x.Contains(" "))
      .Select(y => y.Substring(0, y.LastIndexOf(" ")))
      .Distinct();
  }

  /**
     Return a keymap of the given name (may be null). May opt to
     create one if it doesn't exist.
   */
  public Keymap GetKeymap(string name, bool createIfNecessary = false) {
    var k = keymaps
      .Where(x => x.name == name)
      .SingleOrDefault();
    if (k == null && createIfNecessary) {
      k = new Keymap() { name = name };
      // Add this to the front of the keymaps list.
      // The latest added keymap has the highest precedent.
      k.priority = keymaps.Any()
        ? keymaps
          .Where(km => km.enabled)
          .Max(x => x.priority) + 10
        : 0;
      //keymaps.Insert(0, k);
      keymaps.Add(k);
      keymaps = keymaps.OrderByDescending(x => x.priority).ToList();
      k.keymapChange = KeymapChanged;
    }
    return k;
  }

  /**
     Return a buffer of the given name. May opt to create one if it
     doesn't exist, or it will return null.
   */
  public IBuffer GetBuffer(string name, bool createIfNecessary = false) {
    var b =  buffers
      .Where(k => k.name == name)
      .SingleOrDefault();
    if (createIfNecessary && b == null) {
      b = new Buffer(name);
      buffers.Add(b);
    }
    return b;
  }

  /**
     Convert the command name to its canonical form, e.g.\ PascalCase,
     camelCase, kebab-case, or leave as is.
   */
  public string CanonizeCommand(string command) {
    return ChangeCase.Convert(command, commandCase);
  }

  /**
     Convert the variable name to its canonical form, e.g.\ PascalCase,
     camelCase, kebab-case, or leave as is.
   */
  public string CanonizeVariable(string variable) {
    return ChangeCase.Convert(variable, variableCase);
  }


  /**
     Find all the commands in the given assembly.
   */
  // private Dictionary<string, CommandInfo> FindCommands(string assemblyName
  //                                                      = "Assembly-CSharp") {
  //   // Let's just look at the assembly with the user scripts.
  //   var assembly = Assembly.Load(assemblyName);
  //   return FindCommands(assembly
  //                       .GetTypes(),
  //                       CanonizeCommand,
  //                       GetGroup);
  // }

  protected void RegisterType(Type t) {
    //Debug.Log("RegisterType({0})".Formatted(t));
    var cmds = FindCommands(new [] {t},
                            CanonizeCommand,
                            type => GetGroup(type, true),
                            MakeUnboundDelegate);
    var vars = FindVariables(new [] {t},
                             CanonizeVariable);
    if (! cmds.Any() && ! vars.Any()) {
      Debug.LogWarning("No commands or variables found in class "
                       + t.PrettyName());
    } else {
      commands.AddAll(cmds);
      AddKeyBindings(cmds);
      variables.AddAll(vars);
    }
  }

  private HashSet<int> registeredObjects = new HashSet<int>();
  protected void UnregisterObject(object o) {
    var hashCode = RuntimeHelpers.GetHashCode(o);
    if (! registeredObjects.Contains(hashCode)) {
      // This happens normally on a level change if there's a MinibufferConsole in that other level.
      // It's not _fixable_ for the end user, so maybe we should actually ignore it.
      // Debug.LogWarning("Trying to unregister object '{0}' with hash {1} that's never been registered; ignoring."
      //                  .Formatted(o.ToString(), hashCode));
      return;
    }
    var cmds = FindCommands(new [] {o.GetType()},
                            CanonizeCommand,
                            null,
                            null);
    var vars = FindVariables(new [] {o.GetType()},
                             CanonizeVariable);
    try {
      foreach(string cmd in cmds.Keys)
        commands.Remove(cmd);
      RemoveKeyBindings(cmds);
      foreach(string varname in vars.Keys)
        variables.Remove(varname);
    } catch (NullReferenceException e) {
      throw new MinibufferException("Error unregistering object {0} of type {1}."
                                    .Formatted(o, o.GetType().PrettyName()),
                                    e);
    }
  }

  /*
    Register an instance's commands and variables with %Minibuffer.
  */
  internal void RegisterObject(object o) {
    Type t = o.GetType();
    var boundCmds = FindCommands(new [] {t},
                                 CanonizeCommand,
                                 type => GetGroup(type, true),
                                 z => MakeBoundDelegate(z, o));
    var vars = FindVariables(new [] {t}, CanonizeVariable);
    if (! boundCmds.Any() && ! vars.Any()) {
      Debug.LogWarning("No commands or variables found for object '{0}' in class {1}."
                       .Formatted(o, t.PrettyName()));
    } else {
      var hashCode = RuntimeHelpers.GetHashCode(o);
      // Debug.Log("Registering object '{0}' with hash {1}."
      //           .Formatted(o.ToString(), hashCode));
      registeredObjects.Add(hashCode);
      var boundVars = vars.ToDictionary(kv => kv.Key, kv => kv.Value.ToBoundVariable(o));
      commands.AddAll(boundCmds);
      AddKeyBindings(boundCmds);
      variables.AddAll(boundVars);
    }
  }


  /**
     \name Register Dynamic Commands and Variables
     @{
  */

  /**
     Register a command dynamically.  Command can be implicitly
     converted from a string so

     ```
     minibuffer.RegisterCommand("give-money", (int x) => { money += x; });
     ```

     is equivalent to

     ```
     minibuffer.RegisterCommand(new Command("give-money"), (int x) => { money += x; });
     ```

     \note Behavior for a dynamic command whose action has gone out of
     scope is undefined.  Please use UnregisterCommand for commands
     that have limited scope.
   */
  public void RegisterCommand(Command command,
                              Delegate action) {
    if (command.group == null) {
      command.group = anonymousGroup.name; //"*anonymous*";
    }
    GetGroup(command.group, true);
    var name = CanonizeCommand(command.name);
    var ci = new CommandInfo(command) { delegate_ = action, name = name };
    commands[name] = ci;
    AddKeyBindings(ci); // maybe
  }

  /**
    Register a command dynamically that has no arguments.
  */
  public void RegisterCommand(Command command,
                              Action action) {
    RegisterCommand(command, (Delegate) action);
  }

  /**
     Register a command dynamically that accepts one argument.
   */
  public void RegisterCommand<T>(Command command,
                                 Action<T> action) {
    RegisterCommand(command, (Delegate) action);
  }


  /**
     Register a command dynamically that accepts two arguments.
  */
  public void RegisterCommand<T1, T2>(Command command,
                                      Action<T1, T2> action) {
    RegisterCommand(command, (Delegate) action);
  }

  /**
     Register a command dynamically that accepts three arguments.
  */
  public void RegisterCommand<T1, T2, T3>(Command command,
                                          Action<T1, T2, T3> action) {
    RegisterCommand(command, (Delegate) action);
  }

  /**
     Register a command dynamically that accepts four arguments.
  */
  public void RegisterCommand<T1, T2, T3, T4>(Command command,
                                          Action<T1, T2, T3, T4> action) {
    RegisterCommand(command, (Delegate) action);
  }

  /**
     Remove a command.  Most useful for dynamically registered commands.
   */
  public void UnregisterCommand(string commandName) {
    if (commandName == null)
      throw new NullReferenceException("commandName");
    CommandInfo ci;
    var name = CanonizeCommand(commandName);
    if (commands.TryGetValue(name, out ci)) {
      RemoveKeyBindings(ci);
      commands.Remove(name);
    }
  }
  /**
     Register a variable dynamically.

     \note If the variable is for a field or property, prefer the
     Variable attribute.

     \note Behavior for a dynamic variable whose getters and setters
     have gone out of scope is undefined.  Please use
     UnregisterVariable for variables that have limited scope.
   */
  public void RegisterVariable<T>(Variable variable,
                                  Func<T> getFunc,
                                  Action<T> setFunc,
                                  Type declaringType = null) {
    var name = CanonizeVariable(variable.name);
    if (variables.ContainsKey(name))
      Debug.LogWarning("Overwriting variable '{0}'.".Formatted(name));
    var dvi = new DynamicVariableInfo<T>(variable, getFunc, setFunc);
    if (declaringType != null)
      dvi.DeclaringType = declaringType;
    variables[name] = dvi;
  }

  /**
     Register a variable of class X dynamically.

     Here the getFunc and setFunc receive an extra argument, which is an
     instance of class X. This allows for there to be a Variable with multiple
     instances.  See CommandCompleter implementation for an example.
   */
  public void RegisterVariable<X,T>(Variable variable,
                                    Func<X,T> getFunc,
                                    Action<X,T> setFunc) {
    var name = CanonizeVariable(variable.name);
    if (variables.ContainsKey(name))
      Debug.LogWarning("Overwriting variable '{0}'.".Formatted(name));
    variables[name] = new DynamicVariableInfo<X,T>(variable, getFunc, setFunc);
  }

  /**
     Remove a variable.  Most useful for dynamically registered variables.
   */
  public void UnregisterVariable(string variable) {
    variables.Remove(variable);
  }

  ///@}

  private static Delegate MakeUnboundDelegate(MethodInfo z) {
    try {
      // print("making unbound delegate for method " + z.PrettySignature());
      return Delegate.CreateDelegate(z.CreateDelegateType(true),
                                     z,
                                     true);
    } catch (ArgumentException ae) {
      throw new MinibufferException("Unable to make unbound delegate for method " + z, ae);
      // Debug.LogWarning("Unable to make unbound delegate for method " + z);
      // return null;
    }
  }

  private static Delegate MakeBoundDelegate(MethodInfo z, object instance) {
    if (! z.IsStatic) {
      // print("making bound delegate for method " + z.PrettySignature());
      try {
        return Delegate.CreateDelegate(z.CreateDelegateType(false),
                                       instance,
                                       z,
                                       true);

      } catch (ArgumentException ae) {
        throw new MinibufferException("Unable to make bound delegate for method " + z, ae);
        // Debug.LogWarning("Unable to make bound delegate for method " + z);
        // return null;
      }
    } else {
      return MakeUnboundDelegate(z);
    }
  }

  public GroupInfo GetGroup(Type type, bool createIfNecessary) {
    GroupInfo group;
    var name = CanonizeCommand(type.PrettyName());
    if (! groups.TryGetValue(name, out group)) {
      Group groupAttr = type
        .GetCustomAttributes(typeof(Group), false)
        .Cast<Group>()
        .FirstOrDefault();
      if (groupAttr == null) {
        // if (createIfNecessary)
        groupAttr = new Group(name);
      } else {
        if (groupAttr.name == null)
          groupAttr.name = name;
        groups.TryGetValue(groupAttr.name, out group);
      }
      if (group == null && groupAttr != null && createIfNecessary) {
        groups[groupAttr.name] = group = new GroupInfo(groupAttr);
        if (groupCreated != null)
          groupCreated(group);
        // print("creating group " + group.name + " with tags " + String.Join(", ", group.tags));
      }
    }
    if (group == null) {
      Debug.LogWarning("Wanted to get group for type {0} but none found. Create if necessary marked {1}."
                       .Formatted(type.PrettyName(), createIfNecessary));
    }
    return group;
  }

  public GroupInfo GetGroup(string name, bool createIfNecessary) {
    GroupInfo group;
    if (groups.TryGetValue(name, out group)) {
      return group;
    } else if (createIfNecessary) {
      groups[name] = group = new GroupInfo(new Group(name));
      if (groupCreated != null)
        groupCreated(group);
      return group;
    } else {
      return null;
    }
  }

  public static Dictionary<string, CommandInfo>
    FindCommands(IEnumerable<Type> types,
                 Func<string, string> canonize,
                 Func<Type, GroupInfo> getGroup,
                 Func<MethodInfo, Delegate> createDelegate) {
    // http://www.tallior.com/unity-attributes/

    var commandInfos = types
      .SelectMany(x => x.GetMethods(BindingFlags.DeclaredOnly
                                  | BindingFlags.Public
                                  | BindingFlags.Instance
                                  | BindingFlags.Static))
      .Where(y => y.GetCustomAttributes(typeof(Command), false).Any())
      .Select((MethodInfo z) => {
          Command attr = z.GetCustomAttributes(typeof(Command), false)
                          .Cast<Command>().First();
          if (attr.group == null && getGroup != null) {
            var cgroup = getGroup(z.DeclaringType);
            attr.group = cgroup != null ? cgroup.name : null;
          }
          var commandName = canonize(attr.name ?? z.Name);
          var d = createDelegate != null ? createDelegate(z) : null;
          return new CommandInfo(attr) { name = commandName, delegate_ = d };
        });

    // Handle subcommands
    // ------------------

    // Example: M-x version shows the version of minibuffer. However, there's
    // also the game version, library versions, etc. So perhaps there should be
    // a subcommand dispatch such that a subcommand provides a label, say
    // 'minibuffer', and another command with the same name provides the label,
    // say 'quadrapus-sumo'. Now when M-x version is run, it asks the user for
    // which version: 'minibuffer' or 'quadrapus-sumo'.

    // If there is only one subcommand, it runs automatically.

    // Create a completer $command-subcommands.
    // All commands must have a subcommands label.

    var dupes = commandInfos
      .GroupBy(ci => canonize(ci.name))
      .Where(g => g.Count() > 1);

    if (dupes.Any()) {
      // Degrade gracefully.
      foreach(var group in dupes) {
        Debug.LogError("Command '{0}' defined in multiple methods: "
                       .Formatted(group.Key)
                       + group.Select(ci =>
                                      ci.methodInfo.PrettySignature(true))
                              .OxfordAnd());
      }
      Debug.LogWarning("Will only use one of the available command definitions,"
                       + " undefined which one.");
      commandInfos = commandInfos
        .DistinctBy(ci => canonize(ci.name));
    }
    var commands = commandInfos
      .ToDictionary(w => canonize(w.name));
    return commands;
  }

  /**
    Find variables in the assembly that houses user scripts.  Variables
    are members or properties that have been tagged with the
    [Variable] attribute.
   */
  private Dictionary<string, VariableInfo> FindVariables(string assemblyName
                                                         = "Assembly-CSharp") {
    // Let's just look at the assembly with the user scripts.
    var assembly = Assembly.Load(assemblyName);
    // XXX We should allow other assembly names to be searched too.
    // Should we offer a separate registration mechanism?  Like one that
    // worked for lambdas?
    return FindVariables(assembly.GetTypes(), CanonizeVariable);
  }

  public static Dictionary<string, VariableInfo>
    FindVariables(IEnumerable<Type> types, Func<string,string> canonize) {

    var fields = types
      .SelectMany(x => x.GetFields(BindingFlags.DeclaredOnly
                                 | BindingFlags.Public
                                 | BindingFlags.Instance
                                 | BindingFlags.Static))
      .Where(y => y.GetCustomAttributes(typeof(Variable), false).Any())
      .Select(w => {
          var attr = w.GetCustomAttributes(typeof(Variable), false)
                      .Cast<Variable>().First();
          return new MemberInfo(attr, w);
        });

    var properties = types
      .SelectMany(x => x.GetProperties(BindingFlags.DeclaredOnly
                                     | BindingFlags.Public
                                     | BindingFlags.Instance))
      .Where(y => y.GetCustomAttributes(typeof(Variable), false).Any())
      .Select(w => {
          var attr = w.GetCustomAttributes(typeof(Variable), false)
                      .Cast<Variable>().First();
          return new MemberInfo(attr, w);
        });

    var variableInfos = fields.Concat(properties);

    // Check for dupes.
    var dupes = variableInfos
      .GroupBy(vi => canonize(vi.Name))
      .Where(g => g.Count()>1);
    if (dupes.Any()) {
      // Degrade gracefully.
      foreach(var group in dupes) {
        Debug.LogError("Variable '{0}' defined in multiple fields: "
                         .Formatted(group.Key)
                       + group.Select(vi =>
                                      vi.ToString())
                       .OxfordAnd());
      }
      Debug.LogWarning("Will only use one of the available variable "
                       + "definitions, undefined which one.");
      variableInfos = variableInfos.DistinctBy(vi => canonize(vi.Name));
    }

    var variables = variableInfos
      .Cast<VariableInfo>()
      .ToDictionary(z => canonize(z.Name));
    //print("Variables size " + variables.Count + " for assembly " + assembly);
    //print("Variables found: " + String.Join(",", variables.Select(x => x.Key).ToArray()));
    return variables;
  }

  [Command("self-insert-command",
           description = "Insert the character that provoked this command.",
           group = "editing",
           tags = new [] { "pass-to-inputfield", "no-repeat", "bind-only" })]
  /**
     When editing, most keys call this command which inserts them into
     the minibuffer.
   */
  public void SelfInsertCommand() {
    if (editing
        && ! gui.input.isFocused
        && ! IsInputFieldSelected()) {
      // If we're editing, we're not focused, and we're not focused on another
      // text field then let's focus on our input field.
      ActivateInput();
    }

    char c;
    if (currentKeyChord.hasCharacter) {
      // For some reason this doesn't work, but it really should!
      c = currentKeyChord.character;
    } else {
      if (! currentKeyChord.ForceCharacter(out c)) {
        print("warning: no mapping for '" + currentKeyChord.keyName
              + "' to character.");
        return;
      }
    }
    int count = currentPrefixArg.HasValue ? currentPrefixArg.Value : 1;
    for (int i = 0; i < count; i++) {
      Editing.Append(gui.input, c);
      // Editing.DeselectAll(gui.input);
      // typeof(InputField).GetMethod("Append", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(char) }, null)
        // .Invoke(gui.input, new object[] {c});
      // gui.minibufferPrompt.Insert(c);
      // gui.input.Append(c);
    }
    gui.input.ForceLabelUpdate();
  }

  [Command("universal-argument",
           description = "Pass a numerical or boolean argument to the next command.",
           group = "core",
           keyBinding = "C-u",
           tags = new [] { "no-repeat", "bind-only" })]
  /** \see UniversalArgument */
  public void UniversalArgumentCommand(List<string> keyAccum = null) {
    if (keyAccum == null) {
      keyAccum = new List<string>();
      keyAccum.Add(currentKeyChord.ToString());
    }
    //print("universal-argument");
    ReadKeyChord(k => {
          //print("got key " + k);
          var key = k.ToString();
          int num;
          bool handled;
          if ((Lookup(key) == CanonizeCommand("universal-argument"))
              || int.TryParse(key, out num)
              || key == "-") {
            keyAccum.Add(key);
            if (ReadyForNextMessage())
              Echo(String.Join(" ", keyAccum.ToArray()));
            UniversalArgumentCommand(keyAccum);
            handled = true;
          } else {
            // We hit something that's not a number or part of the
            // universal-argument command, so we stick it back.

            // Let's resolve what the number was.
            if (keyAccum.Where(x => int.TryParse(x, out num)).Any()) {
              // There was a number so let's look at only the numbers.
              currentPrefixArg = keyAccum
              .Where(x => int.TryParse(x, out num))
              .Select(x => int.Parse(x))
              .Aggregate((a,b) => a * 10 + b);
              // Is it Any() or is it an odd number of '-' that makes it negative?
              if (keyAccum.Where(x => x == "-").Any())
                currentPrefixArg *= -1;
            } else {
              currentPrefixArg = Pow(4, keyAccum.Count());
            }
            handled = false;
          }
          return handled;
        });
  }

  // XXX move this somewhere

  private static int Pow(int bas, int exp) {
    return Enumerable
          .Repeat(bas, exp)
          .Aggregate(1, (a, b) => a * b);
  }

  /*
    If the keymaps change in anyway, the prefixes must be updated.
   */
  private void KeymapChanged() {
    prefixes = GetPrefixes(keymaps
                           .Where(v => v.enabled)
                           .SelectMany(x => x.map.Keys)).ToArray();
    keymaps = keymaps.OrderByDescending(x => x.priority).ToList();
  }

  /**
    Return all the key bindings for the given command.
   */
  public IEnumerable<string> KeyBindingsForCommand(string command) {
    var name = CanonizeCommand(command);
    return keymaps
      .SelectMany(y => y.ToDict())
      .Where(x => x.Value == name)
      .Select(x => x.Key);
  }



  // Windows plus the minibuffer visibility.
  private bool[] windowsWereVisible;
  [Command("toggle-visibility",
           description = "Toggle minibuffer visibility.",
           group = "core",
           keyBinding = "~")]
  public void ToggleVisibility([UniversalArgument] bool animate) {
    var anyVisible = visible || gui.main.visible;
    if (anyVisible) {
      if (windowsWereVisible == null)
        windowsWereVisible = new bool[windows.Length + 1];
      // We're going to hide stuff.  Let's remember what was visible.
      for(int i = 0; i < windows.Length; i++) {
        windowsWereVisible[i] = windows[i].visible;
        windows[i].visible = false;
      }
      // It'd be nice if the minibuffer were a Window too.
      windowsWereVisible[windows.Length] = visible;

      // Hide stuff.
      if (animateVisible ^ animate)
        visibleAnim = false;
      else
        visibleNoAnim = false;
    } else {
      // We're going to show stuff.
      if (windowsWereVisible == null) {
        if (animateVisible ^ animate)
          visibleAnim = true;
        else
          visibleNoAnim = true;
      } else {
        for(int i = 0; i < windows.Length; i++) {
          windows[i].visible = windowsWereVisible[i];
        }
        if (animateVisible ^ animate)
          visibleAnim = windowsWereVisible[windows.Length];
        else
          visibleNoAnim = windowsWereVisible[windows.Length];
      }
    }
  }

  [Command("repeat-last-command",
           description = "Repeat the last command",
           group = "core",
           keyBinding = ".",
           tag = "no-repeat")]
  public void RepeatLastCommand([UniversalArgument] int count) {
    var la = lastRepeatable;
    if (la.methodInfo != null
        && la.methodInfo.Name != "RepeatLastCommand") {
      for (int i = 0; i < count; i++) {
        var result = la.methodInfo.Invoke(la.instance,
                                          la.arguments);
        var obj = WithCommandResult(result);
        if (obj != null && la.methodInfo.ReturnType == typeof(IEnumerator)) {
          StartCoroutine((IEnumerator) obj);
        }
      }
    }
  }

  /**
     Display the given buffer.
   */
  public void Display(IBuffer b, bool? wrapText = null) {
    gui.main.buffer = b;
    //Canvas.ForceUpdateCanvases();
    if (wrapText.HasValue) {
      gui.main.wrapText = wrapText.Value;
    }
    gui.main.visible = true;

  }

  /**
     \name History
     @{
  */

  /**
     Get or make a new history.
  */
  internal List<string> GetHistory(string name, bool createIfNecessary = true) {
    if (name == null)
      name = "default-history";
    List<string> result = null;
    if (! histories.TryGetValue(name, out result)
        && createIfNecessary) {
      result = new List<string>();
      histories[name] = result;
    }
    return result;
  }
  // quadrapus-color˜˜~
  // M-n sometimes produces the small tilde, hex code cb9c, unicode hex 2dc, decimal 732
  [Command(description = "Save current input; show next item in history.",
           keymap = "editing",
           keyBindings = new [] { "M-n", "M-uparrow" },
           tag = "bind-only")]
  public void HistoryNext() {
    if (editState.history != null) {
      if (editState.nextHistory.Count > 0) {
        editState.prevHistory.Enqueue(input);
        input = editState.nextHistory.Dequeue();
      } else {
        MessageInline(" [No further history.]");
      }
    }
  }

  [Command(description = "Save current input; show previous item in history.",
           keymap = "editing",
           keyBindings = new [] { "M-p", "M-downarrow" },
           tag = "bind-only")]
  public void HistoryPrevious() {
    if (editState.history != null) {
      if (editState.prevHistory.Count > 0) {
        editState.nextHistory.Enqueue(input);
        input = editState.prevHistory.Dequeue();
      } else {
        MessageInline(" [No previous history.]");
      }
    }
  }

  private void SaveHistory() {
    var historyCount = 10; // Save only the last ten in each history.
    histories.Values
      .Each(h => h.LimitSize(historyCount));
    StringSerializationAPI.Save("$data/histories.json", histories);
  }

  private void LoadHistory() {
    histories
      = StringSerializationAPI
          .Load<Dictionary<string, List<string>>>("$data/histories.json")
        ?? new Dictionary<string, List<string>>();
  }

  [Command("history-clear",
           description = "Clear the history.")]
  public void HistoryClear() {
    histories = new Dictionary<string, List<string>>();
    Message("History cleared.");
  }
  /// @}

  [Command(description = "Copy buffer or input prompt depending. "
           + "Copy the buffer if visible and not editing, otherwise copy the prompt",
           group = "buffer")]
  public void CopyBufferOrPrompt() {
    if (gui.main.visible && ! editing)
      Buffer.CopyToClipboard(gui.main.buffer);
    else
      GUIUtility.systemCopyBuffer = this.input;
      // gui.minibufferPrompt.Copy();
  }

  [Command("noop",
           description = "No Operation (noop) does nothing. Good for ignoring certain keys.",
           tag = "no-echo")]
  public static void Noop() {
    // Do nothing.
  }

}

} // end namespace SeawispHunter.MinibufferConsole

/** \example HelloCommands.cs */
/** \example QuadrapusCommands.cs */
