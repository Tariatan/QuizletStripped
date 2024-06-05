using System.Text;
using NetCoreAudio;

Console.WriteLine("Quiz started!");

new Quizlet().StartQuiz();

Console.ReadKey();
public class Quizlet
{
    private readonly List<Tuple<string, string>> _quiz = new List<Tuple<string, string>>();
    private Tuple<string, string> _currentPair = new Tuple<string, string>("", "");
    private readonly Random _rand = new Random();

    private const string ReadoutPath = "/home/shyshkiv/Documents/QUIZLET/readout/";
    private const string VocabularyPath = "/home/shyshkiv/Documents/QUIZLET/it";

    private readonly Player _player = new Player();

    private int _index;
    
    private enum ReadoutType
    {
        ReadoutWord,
        ReadoutTranslation,
    }

    private ReadoutType _readoutType;

    public Quizlet()
    {
        _player.PlaybackFinished += (_, _) =>
            {
                if (_readoutType is ReadoutType.ReadoutWord)
                    ReadoutTranslation();
                else
                    QuizWord();
            };
    }

    public void StartQuiz()
    {
        this.ReadFile(VocabularyPath);
        this.QuizWord();
    }

    private void ReadFile(string file)
    {
        const int bufferSize = 256;

        using var fileStream = File.OpenRead(file);
        using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, bufferSize);
        var sWord = "";
        var sTranslation = "";
        while (streamReader.ReadLine() is { } line)
        {
            if (line == "")
            {
                continue;
            }

            if(sWord == "")
            {
                sWord = line;
            }
            else if(sTranslation == "")
            {
                sTranslation = line;
            }

            if (sWord == "" || sTranslation == "")
            {
                continue;
            }

            _quiz.Add(new Tuple<string, string>(sWord, sTranslation));
            sWord = "";
            sTranslation = "";
        }
    }

    private void QuizWord()
    {
        //_index = _index + 1 < _quiz.Count ? ++_index : 0;
        _index = _rand.Next(_quiz.Count);
        _currentPair = _quiz[_index];

        ReadoutWord();
    }

    private void ReadoutWord()
    {
        _readoutType = ReadoutType.ReadoutWord;

        var name = _currentPair.Item1.Replace("/", " ");
        PlayAudioFile(ReadoutPath + name + ".mp3");
    }

    private void ReadoutTranslation()
    {
        _readoutType = ReadoutType.ReadoutTranslation;

        var name = _currentPair.Item1.Replace("/", " ");
        PlayAudioFile(ReadoutPath + name + "_d.mp3");
    }

    private void PlayAudioFile(string fileName)
    {
        if(File.Exists(fileName))
        {
            _player.Play(fileName);
        }
        else
        {
            QuizWord();
        }
    }
}
