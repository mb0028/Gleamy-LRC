using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MB28.Music
{
    public class LRCParser
    {
        public List<LyricLine> LyricLines { get; private set; } = new();
        public int Count => LyricLines.Count;
        public float Duration => LyricLines.Last().TimeStomp;
        public bool IsGettingLineInRealtimePossible { get; private set; } = false;

        /// <summary> True if LRCParser has not even find 1 line of lyric text with timestomp </summary>
        public bool Has0Timestomps
        {
            get
            {
                int noTimedLines = 0;
                for (int i = 0; i < Count; i++)
                    if (LyricLines[i].TimeStomp == NO_TIMESTOMP)
                        noTimedLines++;
                return noTimedLines == Count;
            }
        }

        /// <summary>
        /// New LRCParser. use <see cref="LineByAudioPosition"/> or <see cref="LyricLines"/>. to get lyrics with their timestomps
        /// </summary>
        /// <param name="lrcFilePath">Path to lrc file (it works with .txt and other basic text formats too)</param>
        /// <param name="maxLinesToRead"> 
        /// StreamReader.Peek() has some problems and randomly stops! so use this to
        /// set how many lines at max to read. it's safe to set it more than files lines.
        /// just don't set it like 10000 because I can surely say there is no lrc file with that many lines of texts...
        /// 200 is good default value.
        /// </param>
        /// <param name="debug"> If true it logs all lyric's lines to console. </param>
        /// <exception cref="FileNotFoundException"></exception>
        public LRCParser(string lrcFilePath, int maxLinesToRead, bool debug = false)
        {
            if (maxLinesToRead >= 10000)
                throw new Exception("Well...");

            if (string.IsNullOrEmpty(lrcFilePath) || !File.Exists(lrcFilePath))
                throw new FileNotFoundException($"File {lrcFilePath} found", lrcFilePath);

            using StreamReader reader = new(lrcFilePath);
            for (int i = 0; i < maxLinesToRead; i++)
            {
                string line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.IsTimedSection())
                        LyricLines.Add(LyricLine.FromString(line));
                    else if (line.IsTagsSection()) { }
                    else LyricLines.Add(new(-1, line));
                }
            }

            if (debug)
                Console.WriteLine(ToString() + $"\nHas 0 timestomps: {Has0Timestomps}\nCount: {Count}");

            IsGettingLineInRealtimePossible = !Has0Timestomps;
        }

        /// <summary> </summary>
        /// <param name="line0"></param>
        /// <param name="newTimestomp"> -1 = no time   |   -2 = don't change timestomp</param>
        /// <param name="newText"> null = don't change text</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ChangeLine(int line0, float newTimestomp = -2, string newText = null)
        {
            if (line0 > Count)
                throw new InvalidOperationException($"Lyric dosen't have {line0} lines!");

            var old = LyricLines[line0];
            LyricLines[line0] = new(newTimestomp == -2 ? old.TimeStomp : newTimestomp, newText ?? old.Lyric);
            IsGettingLineInRealtimePossible = !Has0Timestomps;
        }

        public void AddBefore(int line, float timestomp, string text)
        {
            if (line < Count)
                LyricLines.Insert(line, new(timestomp, text));
            else
                LyricLines.Add(new(timestomp, text));
            IsGettingLineInRealtimePossible = !Has0Timestomps;
        }

        public void AddAfter(int line, float timestomp, string text)
            => AddBefore(line + 1, timestomp, text);

        /// <summary> Gets lyric by audio position. has overloads for audioPosInSeconds (float) and audioPosInMilisecond (int) </summary>
        public string LineByAudioPosition(float audioPosInSeconds)
        {
            if (IsGettingLineInRealtimePossible)
            {
                if (audioPosInSeconds <= LyricLines.First().TimeStomp)
                    return "";
                if (audioPosInSeconds >= LyricLines.Last().TimeStomp)
                    return LyricLines.Last().Lyric;
                return LyricLines[LyricLines.FindIndex(i => audioPosInSeconds <= i.TimeStomp) - 1].Lyric;
            }
            return "No Lyrics...";
        }
        /// <summary> Gets lyric by audio position. has overloads for audioPosInSeconds (float or double) and audioPosInMilisecond (int) </summary>
        public string LineByAudioPosition(int audioPosInMilisecond) => LineByAudioPosition(audioPosInMilisecond / 1000f);

        public void Save(string path) => File.WriteAllText(path, ToString());
        public async Task SaveAsync(string path) => await File.WriteAllTextAsync(path, ToString());

        /////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            string final = string.Empty;
            int i = 0;
            if (!Has0Timestomps)
            {
                foreach (var item in LyricLines)
                {
                    TimeSpan time = item.TimeStomp == NO_TIMESTOMP ? TimeSpan.Zero : TimeSpan.FromSeconds(item.TimeStomp);
                    if (time < TimeSpan.Zero)
                        time = TimeSpan.Zero;

                    string ms = $"{(int)Math.Round(time.Milliseconds / 10d):D2}";
                    final += $"[{time.Minutes:D2}:{time.Seconds:D2}.{ms}]{item.Lyric}";

                    if (i < Count - 1)
                        final += "\n";
                    i++;
                }
            }
            else
            {
                foreach (var item in LyricLines)
                {
                    final += item.Lyric;
                    if (i < Count - 1)
                        final += "\n";
                    i++;
                }
            }
            return final;
        }
        public override bool Equals(object obj) => obj is LRCParser other && GetHashCode() == other.GetHashCode();
        public override int GetHashCode() => HashCode.Combine(ToString());
        const int NO_TIMESTOMP = -1;
    }


    public readonly struct LyricLine
    {
        public readonly float TimeStomp;
        public readonly string Lyric;

        public LyricLine(float time, string lyric)
        {
            TimeStomp = time;
            Lyric = lyric;
        }

        public static LyricLine FromString(string text)
        {
            var match = LRCParserHelper.LRCLine.Match(text);
            if (match.Success)
            {
                float time = (float.Parse(match.Groups[1].Value) * 60) + float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                return new(time, match.Groups[3].Value);
            }

            var match2 = LRCParserHelper.LRCLineNoWord.Match(text);
            if (match2.Success)
            {
                float time = (float.Parse(match2.Groups[1].Value) * 60) + float.Parse(match2.Groups[2].Value, CultureInfo.InvariantCulture);
                return new(time, "");
            }

            return new(-1, $"ERROR PARSING :( Line: {text}");
        }

    }


    public static class LRCParserHelper
    {
        public static Regex LRCLine = new Regex(@"\[(\d+):(\d+\.\d+)\](\P{IsGreek}+)");
        public static Regex LRCLineNoWord = new Regex(@"\[(\d+):(\d+\.\d+)\]");

        public static bool IsTimedSection(this string lyric)
            => lyric.Contains('[') && lyric.Contains(':') && lyric.Contains('.');

        public static bool IsTagsSection(this string lyric)
            => lyric.Contains("[re:") || lyric.Contains("[ti:") || lyric.Contains("[ar:")
            || lyric.Contains("[al:") || lyric.Contains("[au:") || lyric.Contains("[offset:")
            ;
    }
}
