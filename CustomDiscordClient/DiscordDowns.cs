using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using DiscordSharp.Objects;
using DiscordSharp;

namespace CustomDiscordClient
{
    public class Markdown : DependencyObject
    {
        private List<Uri> attachments;

        private DiscordServer _mentionServer;

        private ContextMenu _contextMenu;

        /// <summary>
        /// maximum nested depth of [] and () supported by the transform; implementation detail
        /// </summary>
        private const int _nestDepth = 6;

        /// <summary>
        /// Tabs are automatically converted to spaces as part of the transform  
        /// this constant determines how "wide" those tabs become in spaces  
        /// </summary>
        private const int _tabWidth = 4;

        private const string _markerUL = @"[*+-]";
        private const string _markerOL = @"\d+[.]";

        private int _listLevel;

        /// <summary>
        /// when true, bold and italic require non-word characters on either side  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        /// 
        public bool StrictBoldItalic { get; set; }

        public ICommand HyperlinkCommand { get; set; }

        public Style DocumentStyle
        {
            get { return (Style)GetValue(DocumentStyleProperty); }
            set { SetValue(DocumentStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DocumentStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DocumentStyleProperty =
            DependencyProperty.Register("DocumentStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public Style Heading1Style
        {
            get { return (Style)GetValue(Heading1StyleProperty); }
            set { SetValue(Heading1StyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Heading1Style.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Heading1StyleProperty =
            DependencyProperty.Register("Heading1Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public Style Heading2Style
        {
            get { return (Style)GetValue(Heading2StyleProperty); }
            set { SetValue(Heading2StyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Heading2Style.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Heading2StyleProperty =
            DependencyProperty.Register("Heading2Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public Style Heading3Style
        {
            get { return (Style)GetValue(Heading3StyleProperty); }
            set { SetValue(Heading3StyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Heading3Style.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Heading3StyleProperty =
            DependencyProperty.Register("Heading3Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public Style Heading4Style
        {
            get { return (Style)GetValue(Heading4StyleProperty); }
            set { SetValue(Heading4StyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Heading4Style.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Heading4StyleProperty =
            DependencyProperty.Register("Heading4Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));



        public Style CodeStyle
        {
            get { return (Style)GetValue(CodeStyleProperty); }
            set { SetValue(CodeStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CodeStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CodeStyleProperty =
            DependencyProperty.Register("CodeStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));




        public Markdown(DiscordServer s, ContextMenu cm)
        {
            //HyperlinkCommand = NavigationCommands.GoToPage;
            HyperlinkCommand = null;
            attachments = new List<Uri>();
            _mentionServer = s;
            _contextMenu = cm;
        }

        public List<Block> Transform(string text, string id)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            text = Normalize(text);
            var document = RunBlockGamut(text);
            var final = new List<Block>();
            foreach (var b in document)
            {
                b.Tag = id;
                b.ContextMenu = _contextMenu;
                if (DiscordClientConfig.DarkTheme)
                    b.Foreground = DiscordClientConfig.DarkThemeForeground;
                final.Add(b);
            }


            return final;
        }

        /// <summary>
        /// Perform transformations that form block-level tags like paragraphs, headers, and list items.
        /// </summary>
        private IEnumerable<Block> RunBlockGamut(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            List<Block> b = DoHeaders(text,
                s1 => DoHorizontalRules(s1,
                    s2 => DoLists(s2,
                    sn => FormParagraphs(sn)))).ToList();
            List<Block> final = new List<Block>();
            foreach (Block bl in b)
            {
                bl.IsEnabled = true;
                bl.Margin = new Thickness(0);
                final.Add(bl);
            }
            //if (attachments.Count > 0)
            //{
            //    foreach (Uri u in attachments)
            //    {
            //        var id = $"{u.Host}\\{new string(u.LocalPath.Take(90).ToArray())}";

            //        using (WebClient wc = new WebClient())
            //        {
            //            string path = $"compact_cache/cocaine/embed/{id}";

            //            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
            //                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

            //            if (!File.Exists(path))
            //            {
            //                try
            //                {
            //                    wc.DownloadFile(u, path);
            //                }
            //                catch
            //                {
            //                    try
            //                    {
            //                        wc.DownloadFile("http://i.imgur.com/zeuxkxP.png", path);
            //                    }
            //                    catch
            //                    {
            //                        //File.Copy("error.png", System.IO.Path.GetFullPath(path));
            //                    }
            //                }
            //            }
            //        }

            //        BitmapImage bi = new BitmapImage(new Uri($"compact_cache\\cocaine\\embed\\{id}", UriKind.Relative));
            //        Image image = new Image() { Source = bi, MaxHeight = 400, StretchDirection = StretchDirection.DownOnly, Stretch = Stretch.Uniform };
            //        image.MouseDown += (s, e) =>
            //        {
            //            Process.Start(new ProcessStartInfo($"compact_cache\\cocaine\\embed\\{id}"));
            //        };
            //        image.Cursor = Cursors.Hand;
            //        InlineUIContainer container = new InlineUIContainer(image);
            //        Paragraph p = new Paragraph(container);
            //        p.Margin = new Thickness(0);
            //        final.Add(p);
            //    }
            //}


            return final;


            //text = DoCodeBlocks(text);
            //text = DoBlockQuotes(text);

            //// We already ran HashHTMLBlocks() before, in Markdown(), but that
            //// was to escape raw HTML in the original Markdown source. This time,
            //// we're escaping the markup we've just created, so that we don't wrap
            //// <p> tags around block-level tags.
            //text = HashHTMLBlocks(text);

            //text = FormParagraphs(text);

            //return text;
        }

        /// <summary>
        /// Perform transformations that occur *within* block-level tags like paragraphs, headers, and list items.
        /// </summary>
        private IEnumerable<Inline> RunSpanGamut(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            List<Inline> b = DoCodeSpans(text,
                s0 => DoHyperlinkSpans(s0,
                s1 => DoItalicsAndBold(s1,
                s2 => DoUsernameSpans(s2,
                s3 => DoChannelSpans(s3,
                DoText))))).ToList();

            List<Inline> final = new List<Inline>();
            foreach (Inline i in b)
            {
                i.IsEnabled = true;
                final.Add(i);
            }

            return final;

            //text = EscapeSpecialCharsWithinTagAttributes(text);
            //text = EscapeBackslashes(text);

            //// Images must come first, because ![foo][f] looks like an anchor.
            //text = DoImages(text);
            //text = DoAnchors(text);

            //// Must come after DoAnchors(), because you can use < and >
            //// delimiters in inline links like [this](<url>).
            //text = DoAutoLinks(text);

            //text = EncodeAmpsAndAngles(text);
            //text = DoItalicsAndBold(text);
            //text = DoHardBreaks(text);

            //return text;
        }

        private static Regex _newlinesLeadingTrailing = new Regex(@"^\n+|\n+\z", RegexOptions.Compiled);
        private static Regex _newlinesMultiple = new Regex(@"\n{2,}", RegexOptions.Compiled);
        private static Regex _leadingWhitespace = new Regex(@"^[ ]*", RegexOptions.Compiled);

        /// <summary>
        /// splits on two or more newlines, to form "paragraphs";    
        /// </summary>
        private IEnumerable<Block> FormParagraphs(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            // split on two or more newlines
            //string[] grafs = _newlinesMultiple.Split(_newlinesLeadingTrailing.Replace(text, ""));

            /*foreach (var g in grafs)
            {
                yield return Create<Paragraph, Inline>(RunSpanGamut(g));
            }*/

            return new List<Block>() { Create<Paragraph, Inline>(RunSpanGamut(text)) };
        }

        private static string _nestedBracketsPattern;

        /// <summary>
        /// Reusable pattern to match balanced [brackets]. See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedBracketsPattern()
        {
            // in other words [this] and [this[also]] and [this[also[too]]]
            // up to _nestDepth
            if (_nestedBracketsPattern == null)
                _nestedBracketsPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", _nestDepth) + RepeatString(
                    @" \]
                    )*"
                    , _nestDepth);
            return _nestedBracketsPattern;
        }

        private static string _nestedParensPattern;

        /// <summary>
        /// Reusable pattern to match balanced (parens). See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedParensPattern()
        {
            // in other words (this) and (this(also)) and (this(also(too)))
            // up to _nestDepth
            if (_nestedParensPattern == null)
                _nestedParensPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^()\s]+      # Anything other than parens or whitespace
                     |
                       \(
                           ", _nestDepth) + RepeatString(
                    @" \)
                    )*"
                    , _nestDepth);
            return _nestedParensPattern;
        }

        private static Regex _anchorInline = new Regex(string.Format(@"
                (                           # wrap whole match in $1
                    \[
                        ({0})               # link text = $2
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $3
                        [ ]*
                        (                   # $4
                        (['""])           # quote char = $5
                        (.*?)               # title = $6
                        \5                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )", GetNestedBracketsPattern(), GetNestedParensPattern()),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown link shortcuts into hyperlinks
        /// </summary>
        /// <remarks>
        /// [link text](url "title") 
        /// </remarks>
        [Obsolete]
        private IEnumerable<Inline> DoAnchors(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            // Next, inline-style links: [link text](url "optional title") or [link text](url "optional title")
            return Evaluate(text, _anchorInline, AnchorInlineEvaluator, defaultHandler);
        }

        [Obsolete]
        private Inline AnchorInlineEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string linkText = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            string title = match.Groups[6].Value;

            //var result = Create<Hyperlink, Inline>(RunSpanGamut(linkText));
            //result.Command = HyperlinkCommand;
            //result.CommandParameter = url;
            return null;
        }

        private static Regex _headerSetext = new Regex(@"
                ^(.+?)
                [ ]*
                \n
                (=+|-+)     # $1 = string of ='s or -'s
                [ ]*
                \n+",
    RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _headerAtx = new Regex(@"
                ^(\#{1,6})  # $1 = string of #'s
                [ ]*
                (.+?)       # $2 = Header text
                [ ]*
                \#*         # optional closing #'s (not counted)
                \n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown headers into HTML header tags
        /// </summary>
        /// <remarks>
        /// Header 1  
        /// ========  
        /// 
        /// Header 2  
        /// --------  
        /// 
        /// # Header 1  
        /// ## Header 2  
        /// ## Header 2 with closing hashes ##  
        /// ...  
        /// ###### Header 6  
        /// </remarks>
        private IEnumerable<Block> DoHeaders(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            return Evaluate<Block>(text, _headerSetext, m => SetextHeaderEvaluator(m),
                s => Evaluate<Block>(s, _headerAtx, m => AtxHeaderEvaluator(m), defaultHandler));
        }

        private Block SetextHeaderEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string header = match.Groups[1].Value;
            int level = match.Groups[2].Value.StartsWith("=") ? 1 : 2;

            //TODO: Style the paragraph based on the header level
            return CreateHeader(level, RunSpanGamut(header.Trim()));
        }

        private Block AtxHeaderEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string header = match.Groups[2].Value;
            int level = match.Groups[1].Value.Length;
            return CreateHeader(level, RunSpanGamut(header));
        }

        public Block CreateHeader(int level, IEnumerable<Inline> content)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            var block = Create<Paragraph, Inline>(content);

            switch (level)
            {
                case 1:
                    if (Heading1Style != null)
                    {
                        block.Style = Heading1Style;
                    }
                    break;

                case 2:
                    if (Heading2Style != null)
                    {
                        block.Style = Heading2Style;
                    }
                    break;

                case 3:
                    if (Heading3Style != null)
                    {
                        block.Style = Heading3Style;
                    }
                    break;

                case 4:
                    if (Heading4Style != null)
                    {
                        block.Style = Heading4Style;
                    }
                    break;
            }

            return block;
        }

        private static Regex _horizontalRules = new Regex(@"
            ^[ ]{0,3}         # Leading space
                ([-*_])       # $1: First marker
                (?>           # Repeated marker group
                    [ ]{0,2}  # Zero, one, or two spaces.
                    \1        # Marker character
                ){2,}         # Group repeated at least twice
                [ ]*          # Trailing spaces
                $             # End of line.
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown horizontal rules into HTML hr tags
        /// </summary>
        /// <remarks>
        /// ***  
        /// * * *  
        /// ---
        /// - - -
        /// </remarks>
        private IEnumerable<Block> DoHorizontalRules(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            return Evaluate(text, _horizontalRules, RuleEvaluator, defaultHandler);
        }

        private Block RuleEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            var line = new Line() { X2 = 1, StrokeThickness = 1.0 };
            var container = new BlockUIContainer(line);
            return container;
        }

        private static string _wholeList = string.Format(@"
            (                               # $1 = whole list
              (                             # $2
                [ ]{{0,{1}}}
                ({0})                       # $3 = first list item marker
                [ ]+
              )
              (?s:.+?)
              (                             # $4
                  \z
                |
                  \n{{2,}}
                  (?=\S)
                  (?!                       # Negative lookahead for another list item marker
                    [ ]*
                    {0}[ ]+
                  )
              )
            )", string.Format("(?:{0}|{1})", _markerUL, _markerOL), _tabWidth - 1);

        private static Regex _listNested = new Regex(@"^" + _wholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _listTopLevel = new Regex(@"(?:(?<=\n\n)|\A\n?)" + _wholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown lists into HTML ul and ol and li tags
        /// </summary>
        private IEnumerable<Block> DoLists(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            // We use a different prefix before nested lists than top-level lists.
            // See extended comment in _ProcessListItems().
            if (_listLevel > 0)
                return Evaluate(text, _listNested, ListEvaluator, defaultHandler);
            else
                return Evaluate(text, _listTopLevel, ListEvaluator, defaultHandler);
        }

        private Block ListEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string list = match.Groups[1].Value;
            string listType = Regex.IsMatch(match.Groups[3].Value, _markerUL) ? "ul" : "ol";

            // Turn double returns into triple returns, so that we can make a
            // paragraph for the last item in a list, if necessary:
            list = Regex.Replace(list, @"\n{2,}", "\n\n\n");

            var resultList = Create<List, ListItem>(ProcessListItems(list, listType == "ul" ? _markerUL : _markerOL));

            resultList.MarkerStyle = listType == "ul" ? TextMarkerStyle.Disc : TextMarkerStyle.Decimal;

            return resultList;
        }

        /// <summary>
        /// Process the contents of a single ordered or unordered list, splitting it
        /// into individual list items.
        /// </summary>
        private IEnumerable<ListItem> ProcessListItems(string list, string marker)
        {
            // The listLevel global keeps track of when we're inside a list.
            // Each time we enter a list, we increment it; when we leave a list,
            // we decrement. If it's zero, we're not in a list anymore.

            // We do this because when we're not inside a list, we want to treat
            // something like this:

            //    I recommend upgrading to version
            //    8. Oops, now this line is treated
            //    as a sub-list.

            // As a single paragraph, despite the fact that the second line starts
            // with a digit-period-space sequence.

            // Whereas when we're inside a list (or sub-list), that line will be
            // treated as the start of a sub-list. What a kludge, huh? This is
            // an aspect of Markdown's syntax that's hard to parse perfectly
            // without resorting to mind-reading. Perhaps the solution is to
            // change the syntax rules such that sub-lists must start with a
            // starting cardinal number; e.g. "1." or "a.".

            _listLevel++;
            try
            {
                // Trim trailing blank lines:
                list = Regex.Replace(list, @"\n{2,}\z", "\n");

                string pattern = string.Format(
                  @"(\n)?                      # leading line = $1
                (^[ ]*)                    # leading whitespace = $2
                ({0}) [ ]+                 # list marker = $3
                ((?s:.+?)                  # list item text = $4
                (\n{{1,2}}))      
                (?= \n* (\z | \2 ({0}) [ ]+))", marker);

                var regex = new Regex(pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
                var matches = regex.Matches(list);
                foreach (Match m in matches)
                {
                    yield return ListItemEvaluator(m);
                }
            }
            finally
            {
                _listLevel--;
            }
        }

        private ListItem ListItemEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string item = match.Groups[4].Value;
            string leadingLine = match.Groups[1].Value;

            if (!String.IsNullOrEmpty(leadingLine) || Regex.IsMatch(item, @"\n{2,}"))
                // we could correct any bad indentation here..
                return Create<ListItem, Block>(RunBlockGamut(item));
            else
            {
                // recursion for sub-lists
                return Create<ListItem, Block>(RunBlockGamut(item));
            }
        }

        private static Regex _codeSpan = new Regex(@"
                    (?<!\\)   # Character before opening ` can't be a backslash
                    (`+)      # $1 = Opening run of `
                    (.+?)     # $2 = The code block
                    (?<!`)
                    \1
                    (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown `code spans` into HTML code tags
        /// </summary>
        private IEnumerable<Inline> DoCodeSpans(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            //    * You can use multiple backticks as the delimiters if you want to
            //        include literal backticks in the code span. So, this input:
            //
            //        Just type ``foo `bar` baz`` at the prompt.
            //
            //        Will translate to:
            //
            //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
            //
            //        There's no arbitrary limit to the number of backticks you
            //        can use as delimters. If you need three consecutive backticks
            //        in your code, use four for delimiters, etc.
            //
            //    * You can use spaces to get literal backticks at the edges:
            //
            //          ... type `` `bar` `` ...
            //
            //        Turns to:
            //
            //          ... type <code>`bar`</code> ...         
            //

            return Evaluate(text, _codeSpan, CodeSpanEvaluator, defaultHandler);
        }

        private static readonly Regex RE_URL = new Regex(@"(?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?");

        private Inline CodeSpanEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string span = match.Groups[2].Value;
            //span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            //span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace

            var result = new Run(span);
            result.FontFamily = new FontFamily("Source Code Pro");
            result.Background = new SolidColorBrush(new System.Windows.Media.Color() { R = 200, G = 200, B = 200, A = 255 });
            if (DiscordClientConfig.DarkTheme)
                result.Foreground = DiscordClientConfig.DarkThemeBackground;
            //result.MouseEnter += (sender, e) =>
            //{
            //    result.Background = new SolidColorBrush(new System.Windows.Media.Color() { R = 0, G = 0, B = 0, A = 0 });
            //};
            //result.MouseLeave += (sender, e) =>
            //{
            //    result.Background = new SolidColorBrush(new System.Windows.Media.Color() { R = 0, G = 0, B = 0, A = 255 });
            //};
            if (CodeStyle != null)
            {
                result.Style = CodeStyle;
            }

            return result;
        }

        private IEnumerable<Inline> DoHyperlinkSpans(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            //    * You can use multiple backticks as the delimiters if you want to
            //        include literal backticks in the code span. So, this input:
            //
            //        Just type ``foo `bar` baz`` at the prompt.
            //
            //        Will translate to:
            //
            //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
            //
            //        There's no arbitrary limit to the number of backticks you
            //        can use as delimters. If you need three consecutive backticks
            //        in your code, use four for delimiters, etc.
            //
            //    * You can use spaces to get literal backticks at the edges:
            //
            //          ... type `` `bar` `` ...
            //
            //        Turns to:
            //
            //          ... type <code>`bar`</code> ...         
            //

            return Evaluate(text, RE_URL, HyperlinkEvaluator, defaultHandler);
        }

        private Inline HyperlinkEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string span = match.Groups[0].Value;
            /*span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace*/
            try
            {
                var linkText = span;
                var url = span;

                Inline i = new Run(linkText);
                List<Inline> ilist = new List<Inline>();
                ilist.Add(i);

                var result = new Hyperlink(i);
                result.IsEnabled = true;
                UriBuilder ubu = new UriBuilder(url);
                var uri = ubu.Uri;
                //if (DiscordClient.ValidExtensions.Any(x => uri.ToString().EndsWith(x)))
                //{
                    attachments.Add(ubu.Uri);
                //}

                result.CommandParameter = null;
                result.Command = null;
                result.Click += (sender, args) => System.Diagnostics.Process.Start(ubu.Uri.ToString());
                return result;

            }
            catch
            {
                var result = new Run(span);

                return result;
            }
        }
        public DiscordClient DiscordClient { get; internal set; }
        private static readonly Regex _username = new Regex(@"<@([0-9\(\)](\d{16}|\d{17}))>", RegexOptions.Compiled);

        private IEnumerable<Inline> DoUsernameSpans(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            //    * You can use multiple backticks as the delimiters if you want to
            //        include literal backticks in the code span. So, this input:
            //
            //        Just type ``foo `bar` baz`` at the prompt.
            //
            //        Will translate to:
            //
            //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
            //
            //        There's no arbitrary limit to the number of backticks you
            //        can use as delimters. If you need three consecutive backticks
            //        in your code, use four for delimiters, etc.
            //
            //    * You can use spaces to get literal backticks at the edges:
            //
            //          ... type `` `bar` `` ...
            //
            //        Turns to:
            //
            //          ... type <code>`bar`</code> ...         
            //

            return Evaluate(text, _username, UsernameEvaluator, defaultHandler);
        }

        private Inline UsernameEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string span = match.Groups[0].Value;
            /*span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace*/
            try
            {
                var start = span.IndexOf("@") + 1;
                var end = span.IndexOf(">", start);
                var userId = span.Substring(start, end - start);

                var u = _mentionServer.members.Find(x=>x.ID == userId);
                if (u == null)
                {
                    Inline erri = new Run("Unknown");
                    return erri;
                }

                Inline i = new Run($"@{u.Username}");
                List<Inline> ilist = new List<Inline>();
                ilist.Add(i);

                var result = new Hyperlink(i);
                result.FontWeight = FontWeights.Bold;
                result.IsEnabled = true;

                System.Windows.Media.Color roleColour = new System.Windows.Media.Color() { R = 0, G = 0, B = 0, A = 255 };
                if (DiscordClientConfig.DarkTheme)
                    roleColour = DiscordClientConfig.ColorDarkForeground;
                u.Roles.ForEach(x =>
                {
                    if(x.position > -1 && x.name != "@everyone")
                    {
                        roleColour.A = 255;
                        roleColour.R = (byte)x.color.R;
                        roleColour.G = (byte)x.color.G;
                        roleColour.B = (byte)x.color.B;
                    }
                });
                result.Foreground = new SolidColorBrush(roleColour);

                result.CommandParameter = null;
                result.Command = null;
                //result.Click += async (sender, args) =>
                //{
                //    await Task.Run(() => MainDiscordClient.DownloadHeads(u));
                //    var card = new UserCard(u) { MaxWidth = 500, MaxHeight = 250 };
                //    Point mousePositionInApp = Mouse.GetPosition(Application.Current.MainWindow);
                //    Point mousePositionInScreenCoordinates = Application.Current.MainWindow.PointToScreen(mousePositionInApp);

                //    card.Left = mousePositionInScreenCoordinates.X;
                //    card.Top = mousePositionInScreenCoordinates.Y;
                //    card.Show();
                //    card.WindowStartupLocation = WindowStartupLocation.Manual;
                //};
                return result;

            }
            catch
            {
                var result = new Run(span);

                return result;
            }
        }

        private static readonly Regex _channel = new Regex(@"<#([0-9\(\)](\d{16}|\d{17}))>", RegexOptions.Compiled);

        private IEnumerable<Inline> DoChannelSpans(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            //    * You can use multiple backticks as the delimiters if you want to
            //        include literal backticks in the code span. So, this input:
            //
            //        Just type ``foo `bar` baz`` at the prompt.
            //
            //        Will translate to:
            //
            //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
            //
            //        There's no arbitrary limit to the number of backticks you
            //        can use as delimters. If you need three consecutive backticks
            //        in your code, use four for delimiters, etc.
            //
            //    * You can use spaces to get literal backticks at the edges:
            //
            //          ... type `` `bar` `` ...
            //
            //        Turns to:
            //
            //          ... type <code>`bar`</code> ...         
            //

            return Evaluate(text, _channel, ChannelEvaluator, defaultHandler);
        }

        private Inline ChannelEvaluator(Match match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            string span = match.Groups[0].Value;
            /*span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace*/
            try
            {
                var start = span.IndexOf("#") + 1;
                var end = span.IndexOf(">", start);
                var channelId = span.Substring(start, end - start);

                var c = _mentionServer.channels.Find(x=>x.ID == channelId);
                if (c == null)
                {
                    Inline erri = new Run("Unknown Channel");
                    return erri;
                }

                Inline i = new Run($"#{c.Name}");
                List<Inline> ilist = new List<Inline>();
                ilist.Add(i);

                var result = new Hyperlink(i);
                result.IsEnabled = true;



                result.CommandParameter = null;
                result.Command = null;
                result.Click += (sender, args) =>
                {
                    //MainDiscordClient.SwitchChannel(c);
                };
                return result;

            }
            catch
            {
                var result = new Run(span);

                return result;
            }
        }

        private static Regex _bold = new Regex(@"(\*\*|__) (?=\S) (.+?[*_]*) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex _strictBold = new Regex(@"([\W_]|^) (\*\*|__) (?=\S) ([^\r]*?\S[\*_]*) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static Regex _italic = new Regex(@"(\*|_) (?=\S) (.+?) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex _strictItalic = new Regex(@"([\W_]|^) (\*|_) (?=\S) ([^\r\*_]*?\S) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown *italics* and **bold** into HTML strong and em tags
        /// </summary>
        private IEnumerable<Inline> DoItalicsAndBold(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            // <strong> must go first, then <em>
            if (StrictBoldItalic)
            {
                return Evaluate<Inline>(text, _strictBold, m => BoldEvaluator(m, 3),
                    s1 => Evaluate<Inline>(s1, _strictItalic, m => ItalicEvaluator(m, 3),
                    s2 => defaultHandler(s2)));
            }
            else
            {
                return Evaluate<Inline>(text, _bold, m => BoldEvaluator(m, 2),
                   s1 => Evaluate<Inline>(s1, _italic, m => ItalicEvaluator(m, 2),
                   s2 => defaultHandler(s2)));
            }
        }

        private Inline ItalicEvaluator(Match match, int contentGroup)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            var content = match.Groups[contentGroup].Value;
            return Create<Italic, Inline>(RunSpanGamut(content));
        }

        private Inline BoldEvaluator(Match match, int contentGroup)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            var content = match.Groups[contentGroup].Value;
            return Create<Bold, Inline>(RunSpanGamut(content));
        }

        private static Regex _outDent = new Regex(@"^[ ]{1," + _tabWidth + @"}", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Remove one level of line-leading spaces
        /// </summary>
        private string Outdent(string block)
        {
            return _outDent.Replace(block, "");
        }

        /// <summary>
        /// convert all tabs to _tabWidth spaces; 
        /// standardizes line endings from DOS (CR LF) or Mac (CR) to UNIX (LF); 
        /// makes sure text ends with a couple of newlines; 
        /// removes any blank lines (only spaces) in the text
        /// </summary>
        private string Normalize(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var output = new StringBuilder(text.Length);
            var line = new StringBuilder();
            bool valid = false;

            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\n':
                        if (valid)
                            output.Append(line);
                        output.Append('\n');
                        line.Length = 0;
                        valid = false;
                        break;
                    case '\r':
                        if ((i < text.Length - 1) && (text[i + 1] != '\n'))
                        {
                            if (valid)
                                output.Append(line);
                            output.Append('\n');
                            line.Length = 0;
                            valid = false;
                        }
                        break;
                    case '\t':
                        int width = (_tabWidth - line.Length % _tabWidth);
                        for (int k = 0; k < width; k++)
                            line.Append(' ');
                        break;
                    case '\x1A':
                        break;
                    default:
                        if (!valid && text[i] != ' ')
                            valid = true;
                        line.Append(text[i]);
                        break;
                }
            }

            if (valid)
                output.Append(line);
            output.Append('\n');

            // add two newlines to the end before return
            return text;
        }

        /// <summary>
        /// this is to emulate what's evailable in PHP
        /// </summary>
        private static string RepeatString(string text, int count)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var sb = new StringBuilder(text.Length * count);
            for (int i = 0; i < count; i++)
                sb.Append(text);
            return sb.ToString();
        }

        private TResult Create<TResult, TContent>(IEnumerable<TContent> content)
            where TResult : IAddChild, new()
        {
            var result = new TResult();
            foreach (var c in content)
            {
                result.AddChild(c);
            }

            return result;
        }

        private IEnumerable<T> Evaluate<T>(string text, Regex expression, Func<Match, T> build, Func<string, IEnumerable<T>> rest)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var matches = expression.Matches(text);
            var index = 0;
            foreach (Match m in matches)
            {
                if (m.Index > index)
                {
                    var prefix = text.Substring(index, m.Index - index);
                    foreach (var t in rest(prefix))
                    {
                        yield return t;
                    }
                }

                yield return build(m);

                index = m.Index + m.Length;
            }

            if (index < text.Length)
            {
                var suffix = text.Substring(index, text.Length - index);
                foreach (var t in rest(suffix))
                {
                    yield return t;
                }
            }
        }

        private static Regex _eoln = new Regex("\\s+");

        public IEnumerable<Inline> DoText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            //var t = _eoln.Replace(text, " ");
            //yield return new Run(t);
            return new List<Run> { new Run(text) };
        }
    }
}