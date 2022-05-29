using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class DamoclesLumberScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public KMBossModule boss;
    public Transform bpush;
    public KMSelectable button;
    public KMSelectable screen;
    public Renderer led;
    public TextMesh disp;
    public Material[] ledmat;

    private string[] exempt;
    private string disptext;
    private List<string> modSolveOrder = new List<string> {};
    private bool org;
    private bool[] disarmed = new bool[3];
    private bool setspeed; 
    private float cyclespeed = 0.5f;
    private IEnumerator dispcycle;

    private int modCount;
    private static string[] modOrder;
    private string[] chosen = new string[2];

    private static int minModuleID;
    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        exempt = boss.GetIgnoredModules("Damocles Lumber", new string[] {
        "+",
        "8",
        "14",
        "42",
        "501",
        "A>N<D",
        "Access Codes",
        "Amnesia",
        "Bamboozling Time Keeper",
        "Black Arrows",
        "Brainf---",
        "Busy Beaver",
        "Channel Surfing",
        "Cookie Jars",
        "Cube Synchronization",
        "Damocles Lumber",
        "Divided Squares",
        "Don't Touch Anything",
        "Doomsday Button",
        "Encryption Bingo",
        "Floor Lights",
        "Forget Any Color",
        "Forget Enigma",
        "Forget Everything",
        "Forget It Not",
        "Forget Me Later",
        "Forget Me Not",
        "Forget Perspective",
        "Forget The Colors",
        "Forget Them All",
        "Forget This",
        "Forget Us Not",
        "Hogwarts",
        "Iconic",
        "Kugelblitz",
        "Multitask",
        "OmegaDestroyer",
        "OmegaForget",
        "Organization",
        "Pow",
        "Password Destroyer",
        "Purgatory",
        "RPS Judging",
        "Security Council",
        "Shoddy Chess",
        "Simon Forgets",
        "Simon's Stages",
        "Souvenir",
        "Tallordered Keys",
        "The Time Keeper",
        "The Troll",
        "The Heart",
        "The Swan",
        "The Twin",
        "The Very Annoying Button",
        "Timing is Everything",
        "Turn The Key",
        "Ultimate Custom Night",
        "Übermodule",
        "Whiteout",
        "Zener Cards"});
        module.OnActivate = Activate;
    }

    private void Activate()
    {
        led.material = ledmat[0];
        if (info.GetSolvableModuleNames().Any(x => !exempt.Contains(x)))
        {
            screen.OnInteract = delegate ()
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, screen.transform);
                screen.AddInteractionPunch(0.3f);
                setspeed ^= true;
                if (setspeed)
                {
                    StartCoroutine("Stopwatch");
                    StopCoroutine(dispcycle);
                }
                else
                    StartCoroutine(dispcycle);
                return false;
            };
            StartCoroutine("StartUp");
        }
        else
            OutOfStages();
    }

    private void OutOfStages()
    {
        StopAllCoroutines();
        disp.text = "";
        Debug.LogFormat("[Damocles Lumber #{0}] No more modules remaining. Press the button at any time.", moduleID);
        button.OnInteract = delegate ()
        {
            if (!moduleSolved)
            {
                button.AddInteractionPunch();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bpush);
                bpush.localPosition = new Vector3(0, 1.2f, 0);
                led.material = ledmat[0];
                module.HandlePass();
                moduleSolved = true;
            }
            return false;
        };
    }

    private IEnumerator ButtonUp()
    {
        disarmed[2] = true;
        yield return new WaitForSeconds(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, bpush);
        bpush.localPosition = new Vector3(0, 1.82f, 0);
        disarmed[2] = false;
    }

    private IEnumerator StartUp()
    {
        if(moduleID == moduleIDCounter)
            modOrder = info.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Distinct().ToArray().Shuffle().ToArray();
        yield return new WaitForSeconds(0.1f);
        org = info.GetSolvableModuleNames().Any(x => x == "Organization") || moduleID - minModuleID >= modOrder.Count() - 1;
        if (org)
            led.material = ledmat[1];
        button.OnInteract = delegate ()
        {
            button.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bpush);
            bpush.localPosition = new Vector3(0, 1.2f, 0);
            if ((!moduleSolved || disarmed[1]) && !disarmed[2])
            {
                if (disarmed[0])
                {
                    if (org)
                    {
                        if (info.GetSolvedModuleNames().Where(x => !exempt.Contains(x)).Count() >= info.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count())
                        {
                            Debug.LogFormat("[Damocles Lumber #{0}] Submission accepted. Module disarmed.", moduleID);
                            module.HandlePass();
                            moduleSolved = true;
                        }
                        else
                        {
                            Debug.LogFormat("[Damocles Lumber #{0}] Submission accepted. Generating new display.", moduleID);
                            StartCoroutine(ButtonUp());
                            AnsGen(true);
                        }
                    }
                    else
                    {
                        Debug.LogFormat("[Damocles Lumber #{0}] Button pressed. Final strike armed.", moduleID);
                        module.HandlePass();                       
                        disarmed[1] = true;
                        moduleSolved = true;
                    }
                    disarmed[0] = false;
                }
                else
                {
                    Debug.LogFormat("[Damocles Lumber #{0}] Invalid button press.", moduleID);
                    module.HandleStrike();
                    StartCoroutine(ButtonUp());
                }
            }
            return false;
        };
        yield return new WaitForSeconds(0.1f);
        minModuleID = moduleIDCounter;
        AnsGen(org);
        dispcycle = DispCycle();
        StartCoroutine(dispcycle);
        string lastSolve = "";
        while (!moduleSolved || disarmed[1])
        {
            yield return new WaitForSeconds(0.33f);
            List<string> solv = info.GetSolvedModuleNames().Where(x => !exempt.Contains(x)).ToList();
            if(solv.Count() > modCount)
            {
                modCount++;
                List<string> solv2 = new List<string>(solv);
                for (int i = 0; i < modSolveOrder.Count; i++)
                    solv2.Remove(modSolveOrder[i]);
                lastSolve = solv2.Last();
                Debug.LogFormat("[Damocles Lumber #{0}] {1} solved.", moduleID, lastSolve);
                modSolveOrder.Add(lastSolve);
                if (disarmed[0])
                {
                    disarmed[0] = false;
                    module.HandleStrike();
                    Debug.LogFormat("[Damocles Lumber #{0}] Button not pressed. Resetting.", moduleID);
                    if (solv.Count() >= info.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count())
                        OutOfStages();
                    else
                    {
                        if (!org)
                        {
                            org = moduleID - minModuleID + modCount >= modOrder.Count() - 1;
                            if (org)
                            {
                                Debug.LogFormat("[Damocles Lumber #{0}] No distinct modules remain. Changing modes.", moduleID);
                                led.material = ledmat[1];
                            }
                        }
                        chosen[1] = "";
                        AnsGen(org);
                    }
                }
                if (disarmed[1])
                {
                    if (!chosen.Contains(lastSolve))
                    {
                        Debug.LogFormat("[Damocles Lumber #{0}] Incorrect module solved. Issuing final strike.", moduleID);
                        module.HandleStrike();
                    }
                    Debug.LogFormat("[Damocles Lumber #{0}] Module disarmed.", moduleID);
                    disarmed[1] = false;
                }
                else if (chosen.Contains(lastSolve))
                {
                    disarmed[0] = true;
                    Debug.LogFormat("[Damocles Lumber #{0}] Displayed module solved. Press the button.", moduleID);
                }
            }
        }
    }

    private void AnsGen(bool x)
    {
        if (org)
        {
            List<string> fulllist = info.GetSolvableModuleNames().Where(k => !exempt.Contains(k)).ToList();
            foreach (string k in info.GetSolvedModuleNames())
                fulllist.Remove(k);
            chosen[0] = fulllist.PickRandom();
        }
        else
        {
            int r = Random.Range(moduleID - minModuleID + modCount, modOrder.Count() - 1);
            chosen[0] = modOrder[r];
            chosen[1] = modOrder[r + 1];
        }
        disptext = new string((chosen[0] + chosen[1]).ToUpperInvariant().Replace(" ", "").ToCharArray().Shuffle());
        Debug.LogFormat("[Damocles Lumber #{0}] The displayed sequence of characters is: {1}", moduleID, disptext);
        if (org)
            Debug.LogFormat("[Damocles Lumber #{0}] The unscrambled module name is {1}.", moduleID, chosen[0]);
        else
            Debug.LogFormat("[Damocles Lumber #{0}] The unscrambled module names are {1} and {2}.", moduleID, chosen[0], chosen[1]);
    }

    private IEnumerator Stopwatch()
    {
        float e = 0;
        while (setspeed)
        {
            yield return null;
            e += Time.deltaTime;
        }
        cyclespeed = e;
    }

    private IEnumerator DispCycle()
    {
        int j = 0;
        while(!moduleSolved || disarmed[1])
        {
            if (j >= disptext.Length)
                j %= disptext.Length;
            disp.text = disptext[j].ToString();
            j++;
            j %= disptext.Length;
            yield return new WaitForSeconds(cyclespeed);
            disp.text = "";
            yield return new WaitForSeconds(0.05f + (j == 0 ? cyclespeed : 0));
        }
    }

    //twitch plays
    bool TwitchShouldCancelCommand;
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press [Presses the button] | !{0} speed <#> [Sets the cycle speed]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            button.OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*speed\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a cycle speed!";
            else if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                float delay;
                if (!float.TryParse(parameters[1], out delay))
                {
                    yield return "sendtochaterror!f The specified speed '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                yield return null;
                screen.OnInteract();
                float t = 0f;
                while (t < delay)
                {
                    t += Time.deltaTime;
                    yield return null;
                    if (TwitchShouldCancelCommand)
                        break;
                }
                screen.OnInteract();
                if (TwitchShouldCancelCommand)
                    yield return "cancelled";
            }
        }
    }

    void TwitchHandleForcedSolve()
    {
        bpush.localPosition = new Vector3(0, 1.2f, 0);
        disp.text = "";
        led.material = ledmat[0];
        module.HandlePass();
        moduleSolved = true;
    }
}