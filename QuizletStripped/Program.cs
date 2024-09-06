//dotnet publish -r linux-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
using System.Diagnostics;
using System.Text;
using NetCoreAudio;
using Timer = System.Timers.Timer;

Console.WriteLine("Quiz started!");

new Quizlet().StartQuiz();

Console.ReadKey();

public class Quizlet
{
    private readonly List<string> files;
    private readonly List<Tuple<string, string>> quiz = new ();
    private Tuple<string, string> currentPair = null!;

    private readonly Timer delayTimer = new ();

    private readonly Player player = new ();
    private readonly Random mRand = new ();

    private const int DelayIntervalMs = 1500;
    private const int BufferSize = 256;

    private const string ModelPathDe = "/home/shyshkiv/SOFT/piper/voices/de_DE-pavoque-low.onnx";
    private const string ConfigPathDe = "/home/shyshkiv/SOFT/piper/voices/de_DE-pavoque-low.onnx.json";

    private const string ModelPathEn = "/home/shyshkiv/SOFT/piper/voices/en_GB-alba-medium.onnx";
    private const string ConfigPathEn = "/home/shyshkiv/SOFT/piper/voices/en_GB-alba-medium.onnx.json";
    private const string PathToPiper = "/home/shyshkiv/SOFT/piper/piper"; // https://github.com/rhasspy/piper?tab=readme-ov-file
    private const string ReadoutDir = "/home/shyshkiv/Documents/QUIZLET/readout/";
    private const string Quiz = "/home/shyshkiv/Documents/QUIZLET/quiz";

    private string modelPath = string.Empty;
    private string configPath = string.Empty;

    private bool isWord = true;

    public Quizlet()
    {
        files = PrepareQuizList();

        delayTimer.Elapsed += (_, _) => OnDelayFinished();
        delayTimer.Interval = DelayIntervalMs;
        delayTimer.Enabled = false;
        delayTimer.AutoReset = false;

        player.PlaybackFinished += (_, _) =>
        {
            delayTimer.Enabled = true;
        };
    }

    public void StartQuiz()
    {
        foreach(var file in files)
        {
            ReadFile(file);
        }

        QuizWord();
    }

    private static List<string> PrepareQuizList()
    {
        List<string> entries = new ();

        using var fileStream = File.OpenRead(Quiz);
        using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);
        while (streamReader.ReadLine()! is { } line)
        {
            entries.Add(line);
        }

        return entries;
    }
    private void OnDelayFinished()
    {
        if (isWord)
        {
            QuizDescription();
        }
        else
        {
            QuizWord();
        }
    }

    private void ReadFile(string file)
    {
        using var fileStream = File.OpenRead(file);
        using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);
        var sWord = "";
        var sDescription = "";
        while (streamReader.ReadLine()! is { } line)
        {
            if(line == "")
            {
                if(sWord != "" && sDescription != "")
                {
                    quiz.Add(new Tuple<string, string>(sWord, sDescription));
                }
                sWord = "";
                sDescription = "";
            }
            if((line != "") && (sWord == ""))
            {
                sWord = line;
            }
            else if((line != "") && (sWord != ""))
            {
                if(sDescription != "")
                {
                    sDescription += "\n";
                }
                sDescription += line;
            }
        }
    }
    private void QuizWord()
    {

        isWord = true;

        currentPair = quiz[mRand.Next(quiz.Count)];

        Console.WriteLine($"{currentPair.Item1}");

        ReadoutWord();
    }
    private void QuizDescription()
    {
        isWord = false;

        Console.WriteLine($"{currentPair.Item2}");

        ReadoutDescription();
    }
    private void ReadoutWord()
    {
        var file = ReadoutDir + currentPair.Item1.Replace("/", " ");
        if (PlayAudioFile(file))
        {
            return;
        }

        this.modelPath = ModelPathDe;
        this.configPath = ConfigPathDe;

        Speak(currentPair.Item1, file);
    }
    private void ReadoutDescription()
    {
        var file = ReadoutDir + currentPair.Item1.Replace("/", " ") + "_d";
        if (PlayAudioFile(file))
        {
            return;
        }

        this.modelPath = ModelPathEn;
        this.configPath = ConfigPathEn;

        Speak(currentPair.Item2, file);
    }
    private bool PlayAudioFile(string fileName)
    {
        var path = fileName + ".mp3";
        if(!File.Exists(path))
        {
            path = fileName + ".wav";
            if (!File.Exists(path))
            {
                return false;
            }
        }

        player.Play(path);

        return true;
    }

    private void Speak(string text, string file)
    {
        // Prepare Piper process
        var psi = new ProcessStartInfo()
        {
            FileName = PathToPiper,
            Arguments = $"-m {this.modelPath} -c {this.configPath} -f \"{file}.wav\"",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Start the Piper process
        using (var piperProcess = Process.Start(psi))
        {
            using (var writer = piperProcess?.StandardInput)
            {
                // Send text input to Piper via stdin
                writer?.WriteLine(text);
            }
            piperProcess?.WaitForExit();
        }

        PlayAudioFile(file);
    }
}
