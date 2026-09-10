if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var original = new System.Collections.Generic.Dictionary<string,int>();
foreach(var key in new[]{"GardenSnake.Best","GardenSnake.HasPlayed"}) if(PlayerPrefs.HasKey(key)) original[key]=PlayerPrefs.GetInt(key);
var report=new System.Collections.Generic.List<string>();
var errors=new System.Collections.Generic.List<string>();
Application.LogCallback log=(message,trace,type)=> { if(type==LogType.Exception || type==LogType.Error) errors.Add(message); };
Application.logMessageReceived+=log;
var primary=GameObject.Find("Primary").GetComponent<UnityEngine.UI.Button>();
var pause=GameObject.Find("Pause").GetComponent<UnityEngine.UI.Button>();
primary.onClick.Invoke();
int stage=0,maxScore=0,turns=0,shots=0;
float start=Time.unscaledTime,nextShot=0,stageAt=start;
GardenSnake.Core.Cell last=controller.Game.Body[0],paused=last;
var lastHeading=controller.Game.Heading;
EditorApplication.CallbackFunction tick=null;
void Finish(string reason) {
    EditorApplication.update-=tick; Application.logMessageReceived-=log;
    report.Add(reason); report.Add("maxScore="+maxScore+" turns="+turns+" frames="+shots+" runtimeErrors="+errors.Count);
    report.AddRange(errors);
    foreach(var key in new[]{"GardenSnake.Best","GardenSnake.HasPlayed"}) { if(original.ContainsKey(key)) PlayerPrefs.SetInt(key,original[key]); else PlayerPrefs.DeleteKey(key); }
    PlayerPrefs.Save();
    System.IO.File.WriteAllLines("Artifacts/presentation-verification.txt",report);
}
tick=()=> {
 try {
    if(!Application.isPlaying || controller==null) {Finish("FAIL Play Mode interrupted");return;}
    var game=controller.Game; float now=Time.unscaledTime;
    maxScore=Mathf.Max(maxScore,game.Score);
    if(now>=nextShot) {nextShot=now+.8f;ScreenCapture.CaptureScreenshot("Artifacts/shots/presentation-"+(shots++).ToString("00")+".png");}
    if(game.Heading!=lastHeading) {turns++;lastHeading=game.Heading;}
    if(stage==0 && maxScore>=2) {pause.onClick.Invoke();paused=game.Body[0];stage=1;stageAt=now;report.Add("PASS start, pickup and growth="+game.Body.Count);return;}
    if(stage==1) {
        if(game.State!=GardenSnake.Core.RunState.Paused || game.Body[0]!=paused) {Finish("FAIL pause moved snake");return;}
        if(now-stageAt<.7f)return;
        pause.onClick.Invoke();report.Add("PASS pause and resume");stage=2;return;
    }
    if(stage<=2 && now-start>27) {stage=3;report.Add("PASS turns="+turns);}
    if(stage==3 && game.State==GardenSnake.Core.RunState.Lost) {stage=4;stageAt=now;report.Add("PASS collision and death");}
    if(stage==4 && now-stageAt>1.4f) {primary.onClick.Invoke();stage=5;stageAt=now;return;}
    if(stage==5 && now-stageAt>.7f) {Finish(game.State==GardenSnake.Core.RunState.Playing && game.Body.Count==3 && errors.Count==0 && maxScore>=2 && turns>=4 ? "PASS restart; presentation flow complete" : "FAIL final state");return;}
    if(now-start>40) {Finish("FAIL timeout");return;}
    if(game.State==GardenSnake.Core.RunState.Lost && stage<3) {stage=3;return;}
    if(game.State==GardenSnake.Core.RunState.Paused) {pause.onClick.Invoke();return;}
    if(stage>=3 || game.State!=GardenSnake.Core.RunState.Playing || game.Body[0]==last) return;
    last=game.Body[0];
    int best=-1,cost=int.MaxValue;
    for(int d=0;d<4;d++) {
        if(d==((int)game.Heading+2)%4) continue;
        var cell=game.Body[0]+GardenSnake.Core.SnakeGame.Offset((GardenSnake.Core.Direction)d);
        if(cell.X<0 || cell.Y<0 || cell.X>=game.Width || cell.Y>=game.Height)continue;
        bool occupied=false;for(int i=0;i<game.Body.Count-1;i++) if(game.Body[i]==cell)occupied=true;
        if(occupied)continue;
        int distance=System.Math.Abs(cell.X-game.Food.X)+System.Math.Abs(cell.Y-game.Food.Y);
        if(distance<cost){cost=distance;best=d;}
    }
    if(best>=0 && best!=(int)game.Heading)controller.Turn(best);
 } catch(System.Exception e) {Finish("FAIL "+e);}
};
EditorApplication.update+=tick;
return "Observing actual controller actions and UI callbacks for 40 seconds maximum";
