using UnityEngine;
namespace HarareAfterHours
{
    public sealed partial class TouchGameplayControls
    {
        // Original vector-style symbols; no copyrighted game artwork or font dependency.
        private void DrawControlBackdrop()
        {
            float s=Scale;
            Draw(new Rect(MoveArea.x-8*s,MoveArea.y-8*s,MoveArea.width+16*s,MoveArea.height+16*s),new Color(.025f,.035f,.075f,.34f),MoveArea.width*.34f);
            if(_player.IsPassenger)return;
            if(!_player.IsInVehicle)
            {
                Rect upper=ButtonRect(Action.Crouch),lower=ButtonRect(Action.Use);
                Rect combat=new(upper.xMin-8*s,upper.yMin-8*s,lower.xMax-upper.xMin+16*s,lower.yMax-upper.yMin+16*s);
                Draw(combat,new Color(.075f,.025f,.035f,.28f),18*s);
                Rect weapon=ButtonRect(Action.SwitchWeapon);
                Draw(new Rect(weapon.x-5*s,weapon.y-4*s,weapon.width+10*s,weapon.height+8*s),new Color(.12f,.055f,.025f,.42f),10*s);
            }
        }

        private void DrawControl(Action action,Rect rect,bool pressed)
        {
            _label??=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            float opacity=ControlOpacity(action);
            bool combat=action is Action.Attack or Action.Fight or Action.Kick or Action.Reload or Action.SwitchWeapon;
            bool direction=IsDirection(action);
            var accent=combat?new Color(1f,.37f,.19f,1):direction?new Color(.30f,.67f,1f,1):new Color(1f,.72f,.32f,1);
            var tint=pressed?Color.white:new Color(.96f,.98f,1f,opacity);
            float radius=action is Action.SwitchWeapon or Action.Perspective or Action.SettingsMenu?9*Scale:rect.width/2;
            Draw(rect,new Color(accent.r,accent.g,accent.b,pressed?1:opacity*.88f),radius);
            var inner=new Rect(rect.x+1.5f*Scale,rect.y+1.5f*Scale,rect.width-3*Scale,rect.height-3*Scale);
            Draw(inner,new Color(.025f,.035f,.065f,pressed ? 0.94f : opacity*.93f),radius);
            var icon=new Rect(rect.x+rect.width*.25f,rect.y+rect.height*.14f,rect.width*.5f,rect.height*.45f);
            bool symbol=action is Action.Attack or Action.View or Action.Jump or Action.Crouch or Action.Reload or Action.Sprint;
            if(symbol)
            {
                if(action==Action.Attack){Stroke(icon,new(.35f,1),new(.7f,.1f),tint);Stroke(icon,new(.55f,1),new(.9f,.1f),tint);Stroke(icon,new(.7f,.1f),new(.85f,0),tint);}
                if(action==Action.View){Stroke(icon,new(0,.5f),new(1,.5f),tint);Stroke(icon,new(.5f,0),new(.5f,1),tint);Draw(new Rect(icon.center.x-4*Scale,icon.center.y-4*Scale,8*Scale,8*Scale),tint,4*Scale);}
                if(action==Action.Reload){for(int i=0;i<9;i++){float a=i*.55f;Stroke(icon,new(.5f+Mathf.Cos(a)*.45f,.5f+Mathf.Sin(a)*.45f),new(.5f+Mathf.Cos(a+.55f)*.45f,.5f+Mathf.Sin(a+.55f)*.45f),tint);}Stroke(icon,new(.95f,.5f),new(.75f,.2f),tint);}
                if(action is Action.Jump or Action.Sprint or Action.Crouch)
                {
                    bool low=action==Action.Crouch;
                    Draw(new Rect(icon.x+icon.width*.45f,icon.y,5*Scale,5*Scale),tint,3*Scale);
                    Stroke(icon,new(.5f,.25f),new(.4f,low?.7f:.6f),tint);
                    Stroke(icon,new(.48f,.3f),new(.9f,.5f),tint);Stroke(icon,new(.48f,.3f),new(.1f,.1f),tint);
                    Stroke(icon,new(.4f,low?.7f:.6f),new(.05f,1),tint);Stroke(icon,new(.4f,low?.7f:.6f),new(.85f,.9f),tint);
                }
            }
            _label.fontSize=Mathf.RoundToInt((action==Action.SwitchWeapon?9:direction?15:9)*Scale);
            Rect labelRect=symbol?new Rect(rect.x,rect.y+rect.height*.60f,rect.width,rect.height*.36f):rect;
            Color old=_label.normal.textColor;_label.normal.textColor=new Color(0,0,0,.85f);
            GUI.Label(new Rect(labelRect.x+Scale,labelRect.y+Scale,labelRect.width,labelRect.height),Label(action),_label);
            _label.normal.textColor=tint;GUI.Label(labelRect,Label(action),_label);_label.normal.textColor=old;
        }
        private static void Stroke(Rect r,Vector2 a,Vector2 b,Color color)
        {
            a=r.position+Vector2.Scale(a,r.size);b=r.position+Vector2.Scale(b,r.size);
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-1,(b-a).magnitude,2),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,0);GUI.matrix=matrix;
        }
    }
}
