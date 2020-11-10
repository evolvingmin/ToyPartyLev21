///*
//  Copyright (c) 2016 Seawisp Hunter, LLC

//  Author: Shane Celis

//using System;
//using System.IO;
//using System.Linq;
//using System.Collections.Generic;
//using System.Threading;
//using NUnit.Framework;
//using UnityEngine;

////using UnityTest;
//using SeawispHunter.MinibufferConsole;
//using SeawispHunter.MinibufferConsole.Extensions;
//using RSG;
///*
//  "I wrote a unit test today. Or yesterday maybe, I don't know. I got
//  the results: 'Tests pass.' That doesn't mean anything."—Albert Camus

//  Albert Camus' #programming status updates were later compiled into a
//  book: The Strange R Language.
//*/
//internal class MinibufferTests {
//  [Test]
//  public void TestTest() {
//    //Assert.Fail();
//  }

//  [Test]
//  /* I wrote a unit test yesterday. I wrote an integration test
//   * today. Tomorrow, I write some documentation. */
//  public void TestPrefixScraping() {
//    var keyBindings
//      = new string[] { "C-x C-f", "C-h f", "C-c x", "C-f", "C-x g", "C-x 5 1" };
//    var prefixes = new string[] { "C-x", "C-h", "C-c", "C-x 5" };
//    CollectionAssert.AreEquivalent((IEnumerable<string>) prefixes, Minibuffer.GetPrefixes(keyBindings));
//  }

//  [Test]
//  public void TestAutoInitProps() {
//    var fc = new FileCompleter();
//    Assert.IsNull(fc.exclude);
//    Assert.AreEqual("*", fc.pattern);

//    var fc2 = new FileCompleter() { exclude = ".meta" };
//    Assert.AreEqual(".meta", fc2.exclude);
//    Assert.AreEqual("*", fc2.pattern);
//  }

//  [Test]
//  public void TestFileCompleter() {
//    var pn = new PathName();
//    var dir = pn.Expand("$temp/minibuffer-tests");
//    // Create the directories we need.
//    var dirs = new string[] { "A", "ABBA", "B", "C" };
//    dirs.Each(d => {
//        var dirname = Path.Combine(dir, d);
//        if (! Directory.Exists(dirname))
//          Directory.CreateDirectory(dirname);
//      });
//    var filename = Path.Combine(dir, "B/test.cs");
//    if (! File.Exists(filename))
//        File.Create(filename);
//    pn.abbrevs["~"] = dir;
//    var fc = new FileCompleter {
//      pathName = pn,
//      exclude = ".meta",
//      pattern = "*" };
//    CollectionAssert.AreEquivalent(new string[] { "~/A/", "~/ABBA/", "~/B/", "~/C/", "~/" },
//                                   fc.Complete("~/"));
//    CollectionAssert.AreEquivalent(new string[] { "~/A/", "~/ABBA/" },
//                                   fc.Complete("~/A"));
//    CollectionAssert.AreEquivalent(new string[] { "~/ABBA/" },
//                                   fc.Complete("~/AB"));
//    CollectionAssert.AreEquivalent(new string[] { "~/B/" },
//                                   fc.Complete("~/B"));
//    CollectionAssert.AreEquivalent(new string[] { "~/B/test.cs", "~/B/"  },
//                                   fc.Complete("~/B/"));
//    CollectionAssert.AreEquivalent(new string[] { "~/" },
//                                   fc.Complete("~"));
//    Assert.IsTrue(
//                                   fc.Complete("/").Any());
//    Assert.IsTrue(
//                                   fc.Complete("").Any());
//  }

//  #if UNITY_WEBGL
//  [Test]
//  public void TestParseQueryString() {
//    CollectionAssert.AreEquivalent(new Dictionary<string,string> {{"hi", "bye"}},
//                                   RunCommands.ParseQueryString("http://localhost:54820/?hi=bye"));
//    CollectionAssert.AreEquivalent(new Dictionary<string,string> {{"playback", "M-x help-me-out"}},
//                                   RunCommands.ParseQueryString("http://localhost:54820/?playback=M-x+help-me-out"));
//    CollectionAssert.AreEquivalent(new Dictionary<string,string> {},
//                                   RunCommands.ParseQueryString("http://localhost:54820/"));
//  }
//  #endif

//  [Test]
//  public void TestKeyChord() {
//    new KeyChord('a').AssertEqual("a");
//    new KeyChord('a') { control = true }.AssertEqual("C-a");
//    new KeyChord('a') { control = true, meta = true }.AssertEqual("C-M-a");
//    KeyChord.FromEvent(Event.KeyboardEvent("return")).AssertEqual("return");
//    KeyChord.FromEvent(Event.KeyboardEvent("^return")).AssertEqual("C-return");
//    KeyChord.FromEvent(Event.KeyboardEvent("%a")).AssertEqual("s-a");
//    var a = KeyChord.FromEvent(Event.KeyboardEvent("%A"));
//    a.AssertEqual("s-a");
//    Assert.IsTrue(a.hasCharacter, "s-a");
//    var ret = KeyChord.FromEvent(Event.KeyboardEvent("^return"));
//    ret.AssertEqual("C-return");
//    Assert.AreEqual("return", ret.keyName);
//    Assert.IsFalse(ret.hasCharacter);
//    Assert.IsTrue(ret.hasKeyName);

//    var Ca = KeyChord.FromString("C-a");
//    Assert.IsTrue(Ca.control);
//    Assert.IsFalse(Ca.hyper);
//    Assert.AreEqual(null, Ca.keyName);
//    Assert.AreEqual('a', Ca.character);

//    Ca = KeyChord.FromString("H-C-a");
//    Assert.IsTrue(Ca.control);
//    Assert.IsTrue(Ca.hyper);
//    Assert.AreEqual(null, Ca.keyName);
//    Assert.AreEqual('a', Ca.character);

//    var Sf = KeyChord.FromString("S-F");
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('F', Sf.character);

//    Sf = KeyChord.FromString("S-f");
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('f', Sf.character);

//    Sf = KeyChord.FromString("F");
//    Assert.IsFalse(Sf.shift);
//    Assert.AreEqual('F', Sf.character);

//    Sf = KeyChord.FromString("S-F").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('F', Sf.character);

//    Sf = KeyChord.FromString("S-f").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('F', Sf.character);

//    Sf = KeyChord.FromString("F").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('F', Sf.character);

//    Sf = KeyChord.FromString(".").Canonical();
//    Assert.IsFalse(Sf.shift);
//    Assert.AreEqual('.', Sf.character);

//    Sf = KeyChord.FromString(">").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('>', Sf.character);

//    Sf = KeyChord.FromString("S-.").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('>', Sf.character);

//    Sf = KeyChord.FromString("S->").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('>', Sf.character);

//    Sf = KeyChord.FromString("!").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('!', Sf.character);

//    Sf = KeyChord.FromString("S-1").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('!', Sf.character);

//    Sf = KeyChord.FromString("S-!").Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('!', Sf.character);

//    var Sreturn = KeyChord.FromString("S-return").Canonical();
//    Assert.IsTrue(Sreturn.shift);
//    Assert.AreEqual("return", Sreturn.keyName);

//    Assert.Throws<MinibufferException>(() => Sreturn = KeyChord.FromString("S-returned").Canonical());

//    Assert
//      .AreEqual("Event:KeyDown   Character:33   Modifiers:None   KeyCode:Exclaim",
//                Event.KeyboardEvent("!").ToString());

//    Assert
//      .AreEqual("Event:KeyDown   Character:\\0   Modifiers:Shift   KeyCode:Alpha1",
//                Event.KeyboardEvent("#1").ToString());

//    // This is the kind of event I actually get when I hit S-1 in the
//    // Unity editor.
//    var e = Event.KeyboardEvent("!");
//    e.keyCode = KeyCode.Alpha1;
//    Assert
//      .AreEqual("Event:KeyDown   Character:33   Modifiers:None   KeyCode:Alpha1",
//                e.ToString());

//    Sf = KeyChord.FromEvent(e);
//    Assert.IsFalse(Sf.shift);
//    Assert.AreEqual('!', Sf.character);
//    Assert.AreEqual("!", Sf.ToString());

//    Sf = KeyChord.FromEvent(e).Canonical();
//    Assert.IsTrue(Sf.shift);
//    Assert.AreEqual('!', Sf.character);
//    Assert.AreEqual("!", Sf.ToString());

//    Assert.AreEqual("C-s", KeyChord.Canonize("C-s"));
//    Assert.AreEqual("C-x C-s", KeyChord.Canonize("C-x C-s"));
//    Assert.AreEqual("C-x C-A", KeyChord.Canonize("C-x C-S-A"));

//    Assert.AreEqual("C-.", KeyChord.Canonize("C-."));
//    Assert.AreEqual("C-.", KeyChord.Canonize("C-period"));
//    Assert.AreEqual("C->", KeyChord.Canonize("C-S-."));

//    KeyChord.standardNotation = true;
//    try {
//    Assert.AreEqual("ctrl-s", KeyChord.Canonize("C-s"));
//    Assert.AreEqual("ctrl-x ctrl-s", KeyChord.Canonize("C-x C-s"));
//    Assert.AreEqual("ctrl-s", KeyChord.Canonize("ctrl-s"));
//    Assert.AreEqual("ctrl-S", KeyChord.Canonize("ctrl-shift-s"));
//    } finally {
//    KeyChord.standardNotation = false;
//    }
//    Assert.IsTrue(KeyChord.IsKeyCode("space"));
//    var space = KeyChord.FromString("space");
//    Assert.AreEqual('\0', space.character);
//    char spaceChar;
//    Assert.IsTrue(space.ForceCharacter(out spaceChar));
//    Assert.AreEqual(' ', spaceChar);
//  }

//  [Test]
//  public void TestPromiseBehavior() {
//    bool fail = false;
//    bool pass = true;

//    EventHandler<ExceptionEventArgs> failFn = (object sender, ExceptionEventArgs e) => {
//      fail = true;
//    };
//    // EventHandler<ExceptionEventArgs> passFn = (object sender, ExceptionEventArgs e) => {
//    //   pass = true;
//    // };
//    Promise.UnhandledException += failFn;
//    var p = new Promise();

//    // Promises don't handle exceptions; they propogate them.  Once a
//    // promise has an error, no Then's are called.  Done will throw
//    // whatever error it has been given regardless of whether it has
//    // been caught or not.
//    p.Catch(ex => pass = true)
//      .Then(() => fail = true)
//      .Catch(ex => pass = true)
//      //.Done(() => pass = true)
//      ;
//    p.Reject(null);
//    if (fail)
//      Assert.Fail();

//    if (! pass)
//      Assert.Fail();

//    //return;
//    /* The following tests aren't quite working because they're running in the
//       promise context (I think). */

//    // fail = false;
//    // pass = true;
//    // p = new Promise();
//    // p.Catch(ex => fail = true)
//    //   .Done(() => pass = true);
//    // p.Resolve();
//    // if (fail)
//    //   Assert.Fail();

//    // p = new Promise();
//    // p.Catch(ex => Assert.Pass())
//    //   .Done(() => Assert.Fail("Should not reach done."));
//    // p.Reject(null);

//    // p = new Promise();
//    // p.Catch(ex => Assert.Fail())
//    //   .Done(() => Assert.Fail("Should not reach done."));
//    // p.Resolve();

//    // var gp = new Promise<int>();
//    // gp.Then(i => {
//    //     if (i == 1)
//    //       throw new Exception("hi");
//    //     else
//    //       return i;
//    //   })
//    //   .Catch(ex =>{
//    //       Debug.Log("Caught " + ex);
//    //       Assert.Fail();
//    //      })
//    //   .Done(i => Assert.Fail());
//    // gp.Resolve(1);
//  }

//  [Test]
//  public void TestCaseConverter() {

//    Assert.AreEqual("hello-world", ChangeCase.PascalCaseToKebabCase("HelloWorld"));
//    Assert.AreEqual("open-db", ChangeCase.PascalCaseToKebabCase("openDB"));

//    // Will it preserve already converted strings?
//    Assert.AreEqual("hello-world", ChangeCase.PascalCaseToKebabCase("hello-world"));
//    Assert.AreEqual("open-db", ChangeCase.PascalCaseToKebabCase("open-db"));

//    // How about the other way.
//    Assert.AreEqual("HelloWorld", ChangeCase.KebabCaseToPascalCase("hello-world"));



//    // We get back a different form. Kebab case has less information.
//    // Could create a registry that has any UPPERCASE words when we do
//    // the first conversion.

//    // The reason this is important is that we want
//    // ExecuteCommand(string commandName) to work regardless of which
//    // case the user has elected to use.
//    Assert.AreEqual("OpenDb", ChangeCase.KebabCaseToPascalCase("open-db"));

//    Assert.AreEqual("HelloWorld", ChangeCase.KebabCaseToPascalCase("HelloWorld"));

//    Assert.AreEqual("OpenDB", ChangeCase.KebabCaseToPascalCase("OpenDB"));
//    Assert.AreEqual("openURL", ChangeCase.KebabCaseToCamelCase("OpenURL"));

//    Assert.AreEqual("hello-world", ChangeCase.PascalCaseToKebabCase("Hello World"));
//    Assert.AreEqual("hello-world", ChangeCase.CamelCaseToKebabCase("Hello World"));
//    Assert.AreEqual("toggle-seawisphunter-logo-button", ChangeCase.CamelCaseToKebabCase("Toggle seawisphunter-logo-button"));
//  }

//  [Test]
//  public void TestCombineCompleters() {
//    var l1 = new ListCompleter("hi");
//    var l2 = new ListCompleter("bye");
//    Assert.AreEqual(1, l1.Count());
//    Assert.AreEqual(1, l2.Count());
//    var l3 = new CombineCompleters(new ICompleter[] { l1, l2 });
//    Assert.AreEqual(2, l3.Count());
//  }

//  [Test]
//  public void TestNumericCompleters() {
//    var nc = new NumberCoercer(typeof(float));
//    Assert.AreEqual(3f, nc.Coerce("3", typeof(float)));
//    Assert.AreEqual(3f, nc.Coerce("3.0", typeof(float)));
//    //Assert.AreEqual(3f, nc.Coerce("3.0", null));
//  }

//  [Test]
//  public void TestFontCharSize() {
//    var font = (Font) new ResourceCompleter<Font>() { showHidden = true }.Coerce("UbuntuMono-B", typeof(Font));
//      //Font.FontWithName("UbuntuMono-B");
//    Assert.IsNotNull(font);
//    CharacterInfo ci;
//    font.GetCharacterInfo('M', out ci, 14);
//    // XXX This changed and I don't know why. I don't believe I use it anywhere.
//    // Assert.AreEqual(0, ci.advance);
//    Assert.AreEqual(7, ci.advance);

//    // https://docs.unity3d.com/ScriptReference/TextGenerator.GetPreferredHeight.html
//    TextGenerationSettings settings = new TextGenerationSettings();
//    settings.textAnchor = TextAnchor.UpperLeft;
//    settings.color = Color.white;
//    settings.generationExtents = new Vector2(500.0F, 200.0F);
//    settings.pivot = Vector2.zero;
//    settings.richText = false;
//    settings.font = font;
//    settings.fontSize = 14;
//    settings.fontStyle = FontStyle.Normal;
//    settings.verticalOverflow = VerticalWrapMode.Overflow;
//    TextGenerator generator = new TextGenerator();
//    generator.Populate("Masdfsdf", settings);
//    //Debug.Log("I generated: " + generator.vertexCount + " verts!");
//    Assert.AreEqual(16f, generator.GetPreferredHeight("M", settings));
//    Assert.AreEqual(8f, generator.GetPreferredWidth("M", settings));

//    Assert.AreEqual(16f, generator.GetPreferredHeight("Me", settings));
//    Assert.AreEqual(16f, generator.GetPreferredWidth("Me", settings));
//  }

//  // [Test]
//  // public void TestIsPrefab() {
//  //   var resources = Resources.FindObjectsOfTypeAll(typeof(Minibuffer)).ToList();
//  //   Assert.AreEqual(1, resources.Count);
//  //}
//  [Test]
//  public void TestFormatTable() {
//    string[][] table = new [] { new[] {"hi", "bye"}};
//    Assert.AreEqual("hi | bye\n-- | ---\n", HelpCommands.FormatTable(table, true));
//    Assert.AreEqual("hi | bye\n", HelpCommands.FormatTable(table, false));

//    table = new [] { new[] {"hi", "bye"}, new [] {"1", "2"}};
//    Assert.AreEqual("hi | bye\n-- | ---\n1  | 2  \n", HelpCommands.FormatTable(table, true));
//    Assert.AreEqual("hi | bye\n1  | 2  \n", HelpCommands.FormatTable(table, false));

//    Assert.AreEqual(2, table.Max((string[] row) => row[0].Length));
//    Assert.AreEqual(3, table.Max((string[] row) => row[1].Length));

//    table = new [] { new[] {"hi", "bye"},
//                     new [] {"1", "1234"},
//                     new [] {"1", "12345"},
//    };

//    Assert.AreEqual("hi | bye  \n1  | 1234 \n1  | 12345\n", HelpCommands.FormatTable(table, false));
//    Assert.AreEqual("hi | bye  \n-- | ---  \n1  | 1234 \n1  | 12345\n", HelpCommands.FormatTable(table, true));
//    Assert.AreEqual(2, table.Max((string[] row) => row[0].Length));
//    Assert.AreEqual(5, table.Max((string[] row) => row[1].Length));
//    // Assert.AreEqual(5, table.Max((string[] row) => row[2].Length));


//    table = new [] { new[] {"hi", "bye", "wat"},
//                     new [] {"1", "1234", "oh"},
//                     new [] {"1", "12345", "huh"},
//    };

//    Assert.AreEqual(2, table.Max((string[] row) => row[0].Length));
//    Assert.AreEqual(5, table.Max((string[] row) => row[1].Length));
//    Assert.AreEqual(3, table.Max((string[] row) => row[2].Length));

//    table = new [] { new[] {"hi", "bye"}, new [] {"1", null}};
//    Assert.Throws<NullReferenceException>(() => HelpCommands.FormatTable(table));
//  }

//  [Test]
//  public void TestPromiseExceptionCatching() {
//    // Do promises catch exceptions well? Yes!
//    bool a = false;
//    bool b = false;
//    Promise p = new Promise();
//    p.Then(() => {
//        throw new NullReferenceException("blah");
//      })
//      .Then(() => a = true)
//      .Catch(ex => b = true);
//    Assert.IsFalse(a);
//    Assert.IsFalse(b);
//    p.Resolve();
//    Assert.IsFalse(a);
//    Assert.IsTrue(b);
//  }

//  [Test]
//  public void TestPromiseSequenceExceptionCatching() {
//    bool a = false;
//    bool b = false;
//    bool c = false;
//    bool d = false;
//    var nearPromises = new List<Func<IPromise>>();
//    nearPromises.Add(() => { a = true; return Promise.Resolved(); });
//    nearPromises.Add(() => { throw new NullReferenceException(); });
//    nearPromises.Add(() => { b = true; return Promise.Resolved(); });

//    Assert.IsFalse(a);
//    Assert.IsFalse(b);
//    Assert.IsFalse(c);
//    Assert.IsFalse(d);
//    Promise.Sequence(nearPromises)
//      .Then(() => c = true)
//      .Catch(ex => d = true);

//    Assert.IsTrue(a);
//    Assert.IsFalse(b);
//    Assert.IsFalse(c);
//    Assert.IsTrue(d);
//  }


//  [Test]
//  public void TestPromiseSequenceExceptionCatchingWithObjects() {
//    bool a = false;
//    bool b = false;
//    bool c = false;
//    bool d = false;
//    var nearPromises = new List<Func<IPromise<object>>>();
//    nearPromises.Add(() => { a = true; return null; });
//    nearPromises.Add(() => { throw new NullReferenceException(); });
//    nearPromises.Add(() => { b = true; return null; });

//    Assert.IsFalse(a);
//    Assert.IsFalse(b);
//    Assert.IsFalse(c);
//    Assert.IsFalse(d);
//    Promise.Sequence(nearPromises)
//      .Then((x) => c = true)
//      .Catch(ex => d = true);

//    Assert.IsTrue(a);
//    Assert.IsFalse(b);
//    Assert.IsFalse(c);
//    Assert.IsTrue(d);
//  }

//  [Test]
//  public void TestMinMaxClamp() {
//    Assert.AreEqual(1, 3.Min(1), "min");
//    Assert.AreEqual(3, 3.Max(1), "max");
//    Assert.AreEqual(0, 0.Clamp(-1, 5));
//    Assert.IsTrue(-2.CompareTo(-1) < 0);

//    Assert.AreEqual(-2, -2.Clamp(-1, 3)); // Doh! -(2).Clamp(-1, 3)
//    Assert.AreEqual(-1, (-2).Clamp(-1, 3));
//  }

//  [Test]
//  public void TestPromiseThenBehavior() {
//    var parent = new Promise();
//    var defer = new Promise();
//    var child = new Promise();
//    bool a = false;
//    bool b = false;
//    var then1
//      = parent
//      .Then(() => a = true)
//      .Then(() => defer)
//      .Then(() => b = true)
//      .Then(() => child);
//    then1
//      .Done();
//    Assert.IsFalse(a);
//    Assert.IsFalse(b);
//    Assert.IsFalse(parent == then1);
//    Assert.IsFalse(child == then1);

//    Assert.IsTrue(parent.IsPending());
//    Assert.IsTrue(defer.IsPending());
//    Assert.IsTrue(child.IsPending());

//    parent.Resolve();
//    Assert.IsFalse(parent.IsPending());
//    Assert.IsTrue(a);
//    Assert.IsFalse(b);
//    Assert.IsTrue(child.IsPending(), "Child has not been run");

//    defer.Resolve();
//    Assert.IsFalse(parent.IsPending());
//    Assert.IsTrue(a);
//    Assert.IsTrue(b);
//    Assert.IsTrue(child.IsPending(), "Child has not been run");
//  }

//  [Test]
//  public void TestPromiseThenBehaviorEarlyDefer() {
//    var parent = new Promise();
//    var defer = new Promise();
//    var child = new Promise();
//    bool a = false;
//    bool b = false;
//    var then1
//      = parent
//      .Then(() => a = true)
//      .Then(() => defer)
//      .Then(() => b = true)
//      .Then(() => child);
//    then1
//      .Done();
//    Assert.IsFalse(a);
//    Assert.IsFalse(b);
//    Assert.IsFalse(parent == then1);
//    Assert.IsFalse(child == then1);

//    Assert.IsTrue(parent.IsPending());
//    Assert.IsTrue(defer.IsPending());
//    Assert.IsTrue(child.IsPending());

//    defer.Resolve();
//    Assert.IsTrue(parent.IsPending());
//    Assert.IsFalse(b);
//    Assert.IsFalse(a);
//    Assert.IsTrue(child.IsPending(), "Child has not been run");

//    parent.Resolve();
//    Assert.IsFalse(parent.IsPending());
//    Assert.IsTrue(a);
//    Assert.IsTrue(b);
//    Assert.IsTrue(child.IsPending(), "Child has not been run");
//  }

//  [Test]
//  public void TestTextProcessing() {
//    string s
//      = "Hello,\n"
//      + "How\n"
//      + "Bad?";
//    Assert.AreEqual(0, s.PositionForLineIndex(0));
//    Assert.AreEqual('H', s[7]);
//    Assert.AreEqual(7, s.PositionForLineIndex(1));
//    Assert.AreEqual('B', s[11]);
//    Assert.AreEqual(11, s.PositionForLineIndex(2));

//    Assert.AreEqual(0, s.LineIndexForPosition(0));
//    Assert.AreEqual(0, s.LineIndexForPosition(5));
//    Assert.AreEqual(1, s.LineIndexForPosition(6));
//    Assert.AreEqual(1, s.LineIndexForPosition(7));
//    Assert.AreEqual(1, s.LineIndexForPosition(9));
//    Assert.AreEqual(2, s.LineIndexForPosition(10));
//    Assert.AreEqual(2, s.LineIndexForPosition(11));
//  }


//}
