using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MB28.Music;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    private static WaitForSecondsRealtime _wait = new(10);
    public GameObject homeUI;
    public GameObject pasteBtn;

    public GameObject lrcUI;
    public GameObject lrcCardTemp;
    public Transform lrcContents;

    public GameObject pickerUI;
    public GameObject pickerCardTemp;
    public Transform pickerContents;
    public bool IsPickerEnable => pickerUI.activeSelf;
    private string lastPickedPath;

    public TMP_InputField inputLyrics;
    public AudioSource audioSource;
    private LRCParser parser;

    public TimeSpan duration;
    public const string READ_MEDIA_AUDIO = "android.permission.READ_MEDIA_AUDIO";
    private static volatile List<string> pathsssss = new();

    void Start()
    {
        Application.targetFrameRate = 120;
        AndroidJavaClass mbJava = new("com.mb28.mbjava.MBJava");
        if (mbJava.CallStatic<int>("CheckPermission", AndroidApplication.currentActivity, READ_MEDIA_AUDIO) != 0)
        {
            mbJava.CallStatic("ShowAlert", AndroidApplication.currentContext, "Permission Rejected!", "App might not work");
            homeUI.SetActive(false);
            mbJava.CallStatic("RequestPermission", AndroidApplication.currentActivity, READ_MEDIA_AUDIO);
        }
        mbJava.Dispose();

        StartCoroutine(Clipboard());
        
        Parallel.ForEach(CommonMusicDirs(), (dir, b) =>
        {
            List<string> dirs = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).ToList();
            dirs.Add(dir);
            Parallel.ForEach(dirs, (subdir, a) =>
            {
                foreach (string path in Directory.GetFiles(subdir, "*.mp3", SearchOption.TopDirectoryOnly))
                    pathsssss.Add(path);
            });
        });
    }

    public async void Select()
    {
        if (IsPickerEnable) return;

        for (int i = 0; i < pickerContents.childCount; i++)
            Destroy(pickerContents.GetChild(i).gameObject);
        for (int i = 0; i < lrcContents.childCount; i++)
            Destroy(lrcContents.GetChild(i).gameObject);

        pickerUI.SetActive(true);
        foreach (string path in pathsssss)
        {
            string ppath = path;
            var g = Instantiate(pickerCardTemp, pickerContents);
            g.GetComponent<Button>().onClick.AddListener(() =>
            {
                lastPickedPath = ppath;
                pickerUI.SetActive(false);
            });
            g.transform.Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text = Path.GetFileNameWithoutExtension(ppath);
            g.SetActive(true);
        }

        while (IsPickerEnable) await Task.Yield();

        if (lastPickedPath == null) return;
        homeUI.SetActive(false);

        DownloadHandlerAudioClip downloader = new($"File://{lastPickedPath}", AudioType.MPEG) { streamAudio = true };
        UnityWebRequest web = new($"File://{lastPickedPath}", "GET", downloader, null);
        await web.SendWebRequest();
        audioSource.clip = DownloadHandlerAudioClip.GetContent(web);
        web.Dispose();

        duration = TimeSpan.FromSeconds(audioSource.clip.length);
        //trackDurationText = string.Format("{0}:{1:D2}", (int)duration.TotalMinutes, duration.Seconds);

        await File.WriteAllTextAsync(Path.Combine(Application.temporaryCachePath, "temp.lrc"), inputLyrics.text);
        parser = new(Path.Combine(Application.temporaryCachePath, "temp.lrc"), 350, true);

        for (int l = 0; l < parser.Count; l++)
        {
            int ii = l;
            GameObject g = Instantiate(lrcCardTemp, lrcContents);
            var card = g.GetComponent<LineCard>();
            card.info.text = $"Line: {ii:D3}";
            card.line.text = parser.LyricLines[ii].Lyric;
            var ts = TimeSpan.FromSeconds(parser.LyricLines[ii].TimeStomp);
            card.time.text = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D2}";
            card.insert.onClick.AddListener(() =>
            {
                float tn = audioSource.time;
                var tss = TimeSpan.FromSeconds(tn);
                card.time.text = $"{tss.Minutes:D2}:{tss.Seconds:D2}.{tss.Milliseconds:D2}";
                parser.ChangeLine(ii, tn);

                g.transform.parent.parent.gameObject.GetComponent<ScrollRect>().verticalNormalizedPosition -= 0.037865f / 6.102f;

                for (int a = 0; a < lrcContents.transform.childCount; a++)
                    lrcContents.GetChild(a).gameObject.GetComponent<LineCard>().border.SetActive(false);
                lrcContents.GetChild(Mathf.Clamp(ii + 1, 0, parser.Count - 1)).gameObject.GetComponent<LineCard>().border.SetActive(true);
            });
            card.now.onClick.AddListener(() => audioSource.time = parser.LyricLines[ii].TimeStomp);
            card.forward.onClick.AddListener(() => {
                float tn = parser.LyricLines[ii].TimeStomp + 0.1f;
                var tss = TimeSpan.FromSeconds(tn);
                card.time.text = $"{tss.Minutes:D2}:{tss.Seconds:D2}.{tss.Milliseconds:D2}";
                parser.ChangeLine(ii, tn);
                audioSource.time = parser.LyricLines[ii].TimeStomp;
            });
            card.backward.onClick.AddListener(() => {
                float tn = parser.LyricLines[ii].TimeStomp - 0.1f;
                var tss = TimeSpan.FromSeconds(tn);
                card.time.text = $"{tss.Minutes:D2}:{tss.Seconds:D2}.{tss.Milliseconds:D2}";
                parser.ChangeLine(ii, tn);
                audioSource.time = parser.LyricLines[ii].TimeStomp;
            });
            
            card.line.onValueChanged.AddListener(s => parser.ChangeLine(ii, -2, s));
            card.time.onValueChanged.AddListener(s => parser.ChangeLine(ii, LyricLine.FromString(s).TimeStomp));
            g.SetActive(true);
        }

        lrcUI.SetActive(true);
    }

    public void Paste() => inputLyrics.text = GUIUtility.systemCopyBuffer;
    public void Copy() => GUIUtility.systemCopyBuffer = inputLyrics.text;
    public void SaveCopy() => GUIUtility.systemCopyBuffer = parser.ToString();
    public void Seek(float second) => audioSource.time = Mathf.Clamp(audioSource.time + second, 0, audioSource.clip.length);
    public void NullifyPicker()
    {
        lastPickedPath = null;
        pickerUI.SetActive(false);
    }
    public void PlayPause()
    {
        if (audioSource.isPlaying)
            audioSource.Pause();
        else
            audioSource.Play();
    }

    private string[] CommonMusicDirs()
    {
        var t = new List<string>();
        if (Application.platform == RuntimePlatform.Android)
        {
            t.Add("/sdcard/Download");
            t.Add("/sdcard/Music");
            t.Add("/sdcard/Documents");
        }
        else
        {
            t.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            t.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        }
        return t.ToArray();
    }
    
    private IEnumerator Clipboard()
    {
        while (true)
        {
            pasteBtn.SetActive(!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer));
            yield return _wait;
        }
    }

}