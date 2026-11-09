#include "app.hpp"
#include <cmath>

namespace breezy {
namespace {
float progress(float time,float start,float end){return std::clamp((time-start)/(end-start),0.f,1.f);}
Gdiplus::PointF between(Gdiplus::PointF a,Gdiplus::PointF b,float p){p=p*p*(3-2*p);return {a.X+(b.X-a.X)*p,a.Y+(b.Y-a.Y)*p};}
void cursor(Gdiplus::Graphics& g,Gdiplus::PointF p,float time,std::initializer_list<float> clicks){using namespace Gdiplus;for(float click:clicks){float age=time-click;if(age<0||age>.55f)continue;float r=6+age*22;Pen ring(Color((BYTE)(160*(1-age/.55f)),94,116,222),2);g.DrawEllipse(&ring,p.X-r,p.Y-r,r*2,r*2);}PointF points[]={{p.X,p.Y},{p.X+1,p.Y+18},{p.X+6,p.Y+13},{p.X+10,p.Y+21},{p.X+14,p.Y+19},{p.X+10,p.Y+11},{p.X+17,p.Y+10}};GraphicsPath path;path.AddPolygon(points,7);Pen outline(Color::White,2.6f);outline.SetLineJoin(LineJoinRound);g.DrawPath(&outline,&path);SolidBrush fill(Color(55,72,108));g.FillPath(&fill,&path);}
}
void SettingsWindow::demo_paint(HWND window,HDC destination){
    using namespace Gdiplus;RECT rect;GetClientRect(window,&rect);int w=rect.right,h=rect.bottom;HDC dc=CreateCompatibleDC(destination);HBITMAP bitmap=CreateCompatibleBitmap(destination,w,h);auto previous=SelectObject(dc,bitmap);FillRect(dc,&rect,(HBRUSH)GetStockObject(WHITE_BRUSH));
    float time=std::fmod(demo_frame_*.033f,8.8f);auto target=get(draft_,section_==0?"ReadTarget":"InputTarget","en");auto source=get(draft_,"InputSourceLanguage","auto");
    auto find=[&](const std::string& code){for(auto& l:app_.strings.languages())if(l.code==code)return l;return app_.strings.languages().front();};auto language=find(target);
    auto label=[&](const std::string& value,RECT r,bool small=false,COLORREF color=RGB(61,77,115)){theme::text(dc,value,r,theme::font(small?10.667f:17.333f),color);};
    auto measure=[&](const std::string& value){auto old=SelectObject(dc,theme::font(17.333f));SIZE size{};auto text=wide(value);GetTextExtentPoint32W(dc,text.c_str(),(int)text.size(),&size);SelectObject(dc,old);return size.cx;};
    {Graphics g(dc);g.SetSmoothingMode(SmoothingModeAntiAlias);g.SetInterpolationMode(InterpolationModeHighQualityBicubic);theme::rounded(g,RectF(.5f,.5f,(float)w-1,(float)h-1),14,Color::White,Color(221,229,244));
        if(section_==0){
            std::string word=target=="en"?find("ja").hello:"Hello";int word_width=measure(word),top=(h-58)/2;PointF origin(24.f,(float)top+17),end((float)24+word_width,(float)top+17),orb((float)24+word_width+25,(float)top+5),speaker((float)w-45,(float)top+26);int popup=std::max((int)orb.X+42,w/2-10);
            float selection=progress(time,.8f,2.2f);if(selection>0){SolidBrush fill(Color(224,232,255));g.FillRectangle(&fill,20.f,(float)top+2,(word_width+9)*selection,34.f);}label(word,{20,top,20+word_width+14,top+36});
            if(time>=2.2f&&app_.image_)g.DrawImage(app_.image_.get(),orb.X-17,orb.Y-17,34.f,34.f);
            if(time>=3.5f){theme::rounded(g,RectF((float)popup,(float)top-10,(float)w-popup-19,78),10,Color(247,249,255),Color(223,231,248));label(word+"  →  "+language.name,{popup+10,top-6,w-46,top+13},true,RGB(143,155,181));label(language.hello,{popup+10,top+16,w-60,top+48});theme::speaker(g,speaker.X-8,speaker.Y-7,time>=5.5f);}
            auto p=time<2.2f?between(origin,end,progress(time,.8f,2.2f)):time<3.5f?between(end,orb,progress(time,2.2f,3.1f)):between(orb,speaker,progress(time,4.2f,5.2f));cursor(g,p,time,{3.2f,5.3f});
        }else{
            auto code=source!="auto"?source:app_.strings.language==target?(target=="en"?"zh-CN":"en"):app_.strings.language;auto original=wide(find(code).example);bool complete=time>=4.7f;int left=18,top=(h-74)/2,right=w-19;
            theme::rounded(g,RectF((float)left,(float)top,(float)w-37,74),8,complete?Color(246,252,250):Color(251,252,255),complete?Color(185,223,209):Color(212,222,246));
            size_t count=(size_t)(original.size()*progress(time,.7f,2.6f));if(count&&count<original.size()&&IS_HIGH_SURROGATE(original[count-1]))count--;auto typed=utf8(original.substr(0,count));label(complete?language.example:typed,{left+10,top+10,right-66,top+64});
            if(!complete&&time<2.7f&&(int)(time*3)%2==0){int x=std::min<LONG>(right-68,left+10+measure(typed)+3);Pen caret(Color(94,116,222));g.DrawLine(&caret,x,top+13,x,top+61);}
            PointF orb((float)right-25,(float)top+20);if(time>.5f&&app_.image_)g.DrawImage(app_.image_.get(),orb.X-17,orb.Y-17,34.f,34.f);if(time>=3.7f&&!complete){Pen spinner(Color(94,116,222),2);g.DrawArc(&spinner,orb.X-18,orb.Y-18,36.f,36.f,std::fmod(time*260,360.f),245.f);}PointF start((float)left+56,(float)top+28);cursor(g,time<2.7f?start:between(start,orb,progress(time,2.7f,3.5f)),time,{.25f,3.55f});
        }
    }
    BitBlt(destination,0,0,w,h,dc,0,0,SRCCOPY);SelectObject(dc,previous);DeleteObject(bitmap);DeleteDC(dc);
}
}
