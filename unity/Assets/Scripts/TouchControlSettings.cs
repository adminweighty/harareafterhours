using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    public sealed partial class TouchGameplayControls
    {
        // v3 deliberately resets the older layout whose missing opacity field
        // could deserialize to zero and make otherwise working controls vanish.
        public const string SettingsKey="Harare.TouchLayout.v3";
        [Serializable] public class Placement { public int action; public float x,y; public float size=1,opacity=.78f; }
        [Serializable] public class Settings
        {
            public float sensitivity=1,buttonScale=1;
            public bool invertY;
            public bool aimOnFire=false,joystickSprint=false;
            public int movementVersion=1;
            public List<Placement> positions=new();
        }
        private Settings _settings=new();private string _beforeEdit;private Action _dragging;private string _editError="";
        public bool Editing {get;private set;}
        private Action _touchDrag;
        private bool _optionsOpen;
        private Rect EditorToolbar=>new(Safe.xMin,Safe.yMin,Safe.width,52*Scale);
        private Action _selected=Action.Attack;
        private static Action LayoutAction(Action action)=>IsDirection(action)?Action.Move:action;
        private float ControlSize(Action action)=>Mathf.Clamp(_settings.positions.Find(p=>p.action==(int)LayoutAction(action))?.size??1,.75f,1.35f);
        private float ControlOpacity(Action action)
        {
            float saved=_settings.positions.Find(p=>p.action==(int)LayoutAction(action))?.opacity??.78f;
            bool essential=action is Action.Up or Action.Down or Action.Left or Action.Right or Action.Attack or Action.SwitchWeapon or Action.Use;
            float minimum=essential ? 0.72f : 0.58f;
            return Mathf.Clamp(saved,minimum,1);
        }
        public float VisibleOpacity(Action action)=>ControlOpacity(action);
        private Placement SelectedPlacement()
        {
            Action action=LayoutAction(_selected);
            var placement=_settings.positions.Find(p=>p.action==(int)action);
            if(placement!=null)return placement;
            var center=action==Action.Move?MoveArea.center:ButtonRect(action).center;
            placement=new Placement{action=(int)action,x=(center.x-Safe.xMin)/Safe.width,y=(center.y-Safe.yMin)/Safe.height};
            _settings.positions.Add(placement);return placement;
        }
        private void UpdateEditorTouch()
        {
            if(_optionsOpen){_touchDrag=Action.None;return;}
            var touch=UnityEngine.InputSystem.Touchscreen.current?.primaryTouch;
            if(touch==null)return;
            Vector2 p=touch.position.ReadValue();p.y=Screen.height-p.y;
            if(EditorToolbar.Contains(p)&&_touchDrag==Action.None)return;
            var phase=touch.phase.ReadValue();
            bool held=phase is UnityEngine.InputSystem.TouchPhase.Began or UnityEngine.InputSystem.TouchPhase.Moved or UnityEngine.InputSystem.TouchPhase.Stationary;
            if(phase==UnityEngine.InputSystem.TouchPhase.Began&&_touchDrag==Action.None)
            {
                _touchDrag=Action.None;
                foreach(var action in _foot)if(action!=Action.SettingsMenu&&ButtonRect(action).Contains(p)){_touchDrag=LayoutAction(action);_selected=_touchDrag;break;}
                if(_touchDrag==Action.None&&MoveArea.Contains(p)){_touchDrag=Action.Move;_selected=Action.Move;}
            }
            if(held&&_touchDrag!=Action.None&&touch.delta.ReadValue().sqrMagnitude>0)
                MoveEditedControl(_touchDrag,p);
            if(!held)_touchDrag=Action.None;
        }
        private void MoveEditedControl(Action action,Vector2 point)
        {
            action=LayoutAction(action);
            var rect=action==Action.Move?MoveArea:ButtonRect(action);
            var center=new Vector2(Mathf.Clamp(point.x,Safe.xMin+rect.width/2,Safe.xMax-rect.width/2),Mathf.Clamp(point.y,Safe.yMin+rect.height/2,Safe.yMax-rect.height/2));
            var placement=_settings.positions.Find(p=>p.action==(int)action);
            if(placement==null){placement=new Placement{action=(int)action};_settings.positions.Add(placement);}
            placement.x=(center.x-Safe.xMin)/Safe.width;placement.y=(center.y-Safe.yMin)/Safe.height;
        }
        private Rect MenuArea=>new(Safe.xMax-160*Scale,Safe.yMin,160*Scale,80*Scale);
        private void LoadSettings()
        {
            try{_settings=JsonUtility.FromJson<Settings>(PlayerPrefs.GetString(SettingsKey,"{}"))??new Settings();}
            catch{_settings=new Settings();}
            // Preserve the user layout while retiring legacy automatic sprint.
            if(!PlayerPrefs.GetString(SettingsKey,"{}").Contains("\"movementVersion\""))_settings.joystickSprint=false;
            _settings.movementVersion=1;
            _settings.sensitivity=float.IsFinite(_settings.sensitivity)?Mathf.Clamp(_settings.sensitivity,.4f,2):1;
            _settings.buttonScale=float.IsFinite(_settings.buttonScale)?Mathf.Clamp(_settings.buttonScale,.85f,1.2f):1;
            _settings.positions??=new();
            _settings.positions.RemoveAll(p=>p==null||!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.size)||!float.IsFinite(p.opacity));
            foreach(var placement in _settings.positions)
            {
                placement.x=Mathf.Clamp01(placement.x);placement.y=Mathf.Clamp01(placement.y);
                placement.size=Mathf.Clamp(placement.size,.75f,1.35f);placement.opacity=Mathf.Clamp(placement.opacity,.58f,1);
            }
            if(!LayoutValid())_settings=new Settings();
        }
        private Vector2 PositionFor(Action action,Vector2 fallback)
        {
            var position=_settings.positions.Find(p=>p.action==(int)action);
            return position==null?fallback:new Vector2(Safe.xMin+Mathf.Clamp01(position.x)*Safe.width,Safe.yMin+Mathf.Clamp01(position.y)*Safe.height);
        }
        public void BeginEdit()
        {
            if(Editing||_player==null||_player.InputLocked||_player.IsInVehicle)return;
            Cancel();_beforeEdit=JsonUtility.ToJson(_settings);Editing=true;Time.timeScale=0;_dragging=Action.None;_optionsOpen=false;_editError="";
        }
        public bool FinishEdit(bool save)
        {
            if(!Editing)return false;
            if(save&&!LayoutValid()){_editError="Separate overlapping controls and keep them inside the screen.";return false;}
            if(save){PlayerPrefs.SetString(SettingsKey,JsonUtility.ToJson(_settings));PlayerPrefs.Save();}
            else _settings=JsonUtility.FromJson<Settings>(_beforeEdit);
            Editing=false;Cancel();if(!_player.InputLocked)Time.timeScale=1;
            return true;
        }
        public void ResetLayout(){_settings=new Settings();_editError="";}
        public bool LayoutValid()
        {
            var rects=new List<Rect>{MoveArea};foreach(var action in _foot)if(!IsDirection(action))rects.Add(ButtonRect(action));
            for(int i=0;i<rects.Count;i++)
            {
                var r=rects[i];if(r.xMin<Safe.xMin||r.xMax>Safe.xMax||r.yMin<Safe.yMin||r.yMax>Safe.yMax||r.Overlaps(MenuArea))return false;
                for(int j=0;j<i;j++)if(r.Overlaps(rects[j]))return false;
            }
            return true;
        }
        private void DrawEditor()
        {
            float s=Scale;var e=Event.current;
            GUI.Box(Safe,GUIContent.none);
            var style=new GUIStyle(GUI.skin.button){fontSize=Mathf.RoundToInt(12*s)};
            var textStyle=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(12*s),wordWrap=true};
            float top=Safe.yMin+8*s;
            if(GUI.Button(new Rect(Safe.xMin+10*s,top,85*s,36*s),"CANCEL",style))FinishEdit(false);
            if(GUI.Button(new Rect(Safe.xMin+105*s,top,85*s,36*s),"RESET",style))ResetLayout();
            if(GUI.Button(new Rect(Safe.xMin+200*s,top,100*s,36*s),"CONFIRM",style))FinishEdit(true);
            if(GUI.Button(new Rect(Safe.xMin+310*s,top,100*s,36*s),"OPTIONS",style)){_optionsOpen=!_optionsOpen;_dragging=_touchDrag=Action.None;}
            if(!Editing)return;
            GUI.Label(new Rect(Safe.xMin+10*s,top+40*s,Safe.width-20*s,44*s),string.IsNullOrEmpty(_editError)?"Drag a control to move it. Select one, then OPTIONS for size and opacity.":_editError,textStyle);
            if(_optionsOpen)
            {
                float x=Safe.center.x-165*s,y=Safe.center.y-70*s;
                Draw(new Rect(x-12*s,y-12*s,354*s,214*s),new Color(.035f,.04f,.05f,.98f),8*s);
                GUI.Label(new Rect(x,y,220*s,25*s),$"Look sensitivity {_settings.sensitivity:F1}×",textStyle);
                _settings.sensitivity=GUI.HorizontalSlider(new Rect(x,y+30*s,200*s,20*s),_settings.sensitivity,.4f,2);
                _settings.invertY=GUI.Toggle(new Rect(x+220*s,y+10*s,110*s,30*s),_settings.invertY,"Invert Y");
                _settings.aimOnFire=GUI.Toggle(new Rect(x,y+58*s,180*s,25*s),_settings.aimOnFire,"Aim when firing");
                _settings.joystickSprint=GUI.Toggle(new Rect(x+190*s,y+58*s,140*s,25*s),_settings.joystickSprint,"Analog sprint");
                var selected=SelectedPlacement();
                GUI.Label(new Rect(x,y+90*s,330*s,25*s),$"{_selected}: size {selected.size:F2}× / opacity {selected.opacity:P0}",textStyle);
                selected.size=GUI.HorizontalSlider(new Rect(x,y+120*s,145*s,20*s),selected.size,.75f,1.35f);
                selected.opacity=GUI.HorizontalSlider(new Rect(x+175*s,y+120*s,145*s,20*s),selected.opacity,.25f,1);
                if(GUI.Button(new Rect(x+100*s,y+155*s,130*s,32*s),"BACK TO LAYOUT",style))_optionsOpen=false;
                return;
            }
            var editActions=new List<Action>{Action.Move};foreach(var action in _foot)if(!IsDirection(action))editActions.Add(action);
            foreach(var action in editActions)
            {
                if(action==Action.SettingsMenu)continue;
                var rect=action==Action.Move?MoveArea:ButtonRect(action);
                if(action==Action.Move)GUI.Box(rect,"MOVE",style);else DrawControl(action,rect,action==_selected);
                if(e.type==EventType.MouseDown&&rect.Contains(e.mousePosition)){_dragging=action;_selected=action;e.Use();}
            }
            if(e.type==EventType.MouseDrag&&_dragging!=Action.None)
            {
                MoveEditedControl(_dragging,e.mousePosition);e.Use();
            }
            if(e.type==EventType.MouseUp)_dragging=Action.None;
        }
    }
}
