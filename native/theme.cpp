#include "theme.hpp"
#include <uxtheme.h>
#include <cmath>

namespace breezy::theme {
static std::map<std::wstring,HFONT> fonts;
HFONT font(float pixels,bool bold,const wchar_t* family){
    auto key=std::wstring(family)+std::to_wstring(pixels)+(bold?L"b":L"n");auto found=fonts.find(key);if(found!=fonts.end())return found->second;
    LOGFONTW value{};value.lfHeight=-(LONG)std::lround(pixels*GetDpiForSystem()/96.f);value.lfWeight=bold?FW_BOLD:FW_NORMAL;value.lfQuality=CLEARTYPE_QUALITY;value.lfCharSet=std::wstring(family)==L"Microsoft YaHei UI"?GB2312_CHARSET:DEFAULT_CHARSET;wcsncpy_s(value.lfFaceName,family,_TRUNCATE);return fonts[key]=CreateFontIndirectW(&value);
}
void release_fonts(){for(auto& item:fonts)DeleteObject(item.second);fonts.clear();}
void rounded(Gdiplus::Graphics& g,Gdiplus::RectF r,float radius,Gdiplus::Color fill,Gdiplus::Color border){
    using namespace Gdiplus;float d=std::min(radius*2,std::min(r.Width,r.Height));GraphicsPath p;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.GetRight()-d,r.Y,d,d,270,90);p.AddArc(r.GetRight()-d,r.GetBottom()-d,d,d,0,90);p.AddArc(r.X,r.GetBottom()-d,d,d,90,90);p.CloseFigure();SolidBrush brush(fill);g.FillPath(&brush,&p);if(border.GetA()){Pen pen(border);g.DrawPath(&pen,&p);}
}
void text(HDC dc,const std::string& value,RECT rect,HFONT face,COLORREF color,UINT flags){auto old=SelectObject(dc,face);SetBkMode(dc,TRANSPARENT);SetTextColor(dc,color);DrawTextW(dc,wide(value).c_str(),-1,&rect,flags|DT_NOPREFIX);SelectObject(dc,old);}
void speaker(Gdiplus::Graphics& g,float x,float y,bool playing){using namespace Gdiplus;Pen pen(playing?Color(94,116,222):Color(143,155,181),1.6f);pen.SetLineJoin(LineJoinRound);PointF points[]={{x,y+4},{x+4,y+4},{x+8,y},{x+8,y+16},{x+4,y+12},{x,y+12}};g.DrawPolygon(&pen,points,6);g.DrawArc(&pen,x+7,y+3,9.f,10.f,-60,120);if(playing)g.DrawArc(&pen,x+7,y-1,15.f,18.f,-55,110);}
enum Kind {button,combo,edit};
struct Surface {Kind kind;bool hover=false,flat=false,toggle=false,outline=false,link=false;};
static void paint(HWND w,HDC dc,Surface& s){
    using namespace Gdiplus;RECT r;GetClientRect(w,&r);FillRect(dc,&r,(HBRUSH)GetStockObject(WHITE_BRUSH));Graphics g(dc);g.SetSmoothingMode(SmoothingModeAntiAlias);
    bool enabled=IsWindowEnabled(w),pressed=SendMessageW(w,BM_GETSTATE,0,0)&BST_PUSHED;auto caption=window_text(w);int id=GetDlgCtrlID(w);auto face=(HFONT)SendMessageW(w,WM_GETFONT,0,0);COLORREF ink=enabled?RGB(84,98,148):RGB(174,183,201);
    if(s.kind==button){
        auto type=GetWindowLongPtrW(w,GWL_STYLE)&BS_TYPEMASK;
        if(s.toggle){int track=32,x=r.right-track-1,y=(r.bottom-17)/2;bool on=SendMessageW(w,BM_GETCHECK,0,0)==BST_CHECKED;rounded(g,RectF((float)x,(float)y,32,17),8,on?Color(100,120,222):Color(220,226,236));SolidBrush thumb(Color::White);g.FillEllipse(&thumb,(float)(x+(on?17:2)),(float)y+2,13.f,13.f);r.right=x-10;ink=RGB(116,127,150);}
        else if(type==BS_AUTOCHECKBOX||type==BS_CHECKBOX){int y=(r.bottom-15)/2;bool on=SendMessageW(w,BM_GETCHECK,0,0)==BST_CHECKED;rounded(g,RectF(.5f,(float)y,14,14),3,on?Color(100,120,222):Color::White,Color(193,203,222));if(on){Pen tick(Color::White,1.7f);g.DrawLine(&tick,3.f,(float)y+7,6.f,(float)y+10);g.DrawLine(&tick,6.f,(float)y+10,11.f,(float)y+4);}r.left+=23;ink=RGB(61,77,115);}
        else if(id==109){speaker(g,(r.right-18)/2.f,(r.bottom-16)/2.f);return;}
        else{
            bool primary=id==901||id==5001||id==6001;Color fill=primary?Color(89,110,222):s.flat||s.outline?Color::White:id==902?Color(237,241,248):Color(245,247,252);if(!enabled)fill=Color(245,247,250);else if(s.hover||pressed)fill=primary?Color(105,121,230):Color(242,245,253);rounded(g,RectF(.5f,.5f,(float)r.right-1,(float)r.bottom-1),s.outline?1:12,fill,s.outline?Color(enabled?0:210,enabled?0:216,enabled?0:228):Color(0,0,0,0));if(primary&&enabled)ink=RGB(255,255,255);if(s.outline&&enabled)ink=RGB(0,0,0);
        }
        if(id==IDCANCEL||id==903)ink=RGB(160,167,181);if(id==1091||id==1092)ink=RGB(143,155,181);
        text(dc,caption,r,face,ink,(s.link||s.toggle||type==BS_AUTOCHECKBOX||type==BS_CHECKBOX?DT_LEFT:DT_CENTER)|DT_VCENTER|DT_SINGLELINE|DT_END_ELLIPSIS);
        if(GetFocus()==w){RECT focus=r;InflateRect(&focus,-3,-3);DrawFocusRect(dc,&focus);}return;
    }
    if(s.kind==combo){
        // The real combo remains responsible for keyboard navigation and accessibility.
        RECT arrow{r.right-21,0,r.right,r.bottom};HBRUSH brush=CreateSolidBrush(RGB(245,247,251));FillRect(dc,&arrow,brush);DeleteObject(brush);Pen p(Color(112,125,151),1.2f);float x=(float)r.right-15,y=r.bottom/2.f-1;g.DrawLine(&p,x,y,x+4,y+4);g.DrawLine(&p,x+4,y+4,x+8,y);r.left+=4;r.right-=24;text(dc,caption,r,face,enabled?RGB(39,46,61):RGB(174,183,201));return;
    }
}
static LRESULT CALLBACK surface_proc(HWND w,UINT m,WPARAM a,LPARAM b,UINT_PTR,DWORD_PTR data){auto s=(Surface*)data;
    if(m==WM_NCDESTROY){RemoveWindowSubclass(w,surface_proc,1);delete s;return DefSubclassProc(w,m,a,b);}
    if(m==WM_MOUSEMOVE&&!s->hover){s->hover=true;TRACKMOUSEEVENT track{sizeof(track),TME_LEAVE,w,0};TrackMouseEvent(&track);InvalidateRect(w,nullptr,FALSE);}
    if(m==WM_MOUSELEAVE){s->hover=false;InvalidateRect(w,nullptr,FALSE);}
    if(s->kind!=edit&&(m==WM_PAINT||m==WM_PRINTCLIENT)){PAINTSTRUCT ps;HDC dc=m==WM_PAINT?BeginPaint(w,&ps):(HDC)a;paint(w,dc,*s);if(m==WM_PAINT)EndPaint(w,&ps);return 0;}
    if(s->kind==edit&&m==WM_NCPAINT){auto result=DefSubclassProc(w,m,a,b);HDC dc=GetWindowDC(w);RECT r;GetWindowRect(w,&r);OffsetRect(&r,-r.left,-r.top);HBRUSH brush=CreateSolidBrush(RGB(216,223,237));FrameRect(dc,&r,brush);DeleteObject(brush);ReleaseDC(w,dc);return result;}
    if(m==WM_ERASEBKGND&&s->kind!=edit)return 1;
    auto result=DefSubclassProc(w,m,a,b);if(m==WM_SETTEXT||m==BM_SETCHECK||m==WM_ENABLE||m==CB_SETCURSEL||m==WM_SETFOCUS||m==WM_KILLFOCUS||m==BM_SETSTATE)InvalidateRect(w,nullptr,FALSE);return result;
}
void style(HWND w,const wchar_t* type){
    std::wstring name(type);if(name==L"BUTTON"||name==WC_COMBOBOXW||name==L"EDIT"){
        auto s=new Surface{ name==L"BUTTON"?button:name==WC_COMBOBOXW?combo:edit};SetWindowTheme(w,L"",L"");SetWindowSubclass(w,surface_proc,1,(DWORD_PTR)s);
        if(s->kind==combo&&((GetWindowLongPtrW(w,GWL_STYLE)&3)==CBS_DROPDOWN)){COMBOBOXINFO info{sizeof(info)};GetComboBoxInfo(w,&info);if(info.hwndItem)SendMessageW(info.hwndItem,EM_SETMARGINS,EC_LEFTMARGIN|EC_RIGHTMARGIN,MAKELPARAM(4,2));}
    }
}
static Surface* surface(HWND w){DWORD_PTR data=0;return GetWindowSubclass(w,surface_proc,1,&data)?(Surface*)data:nullptr;}
void flat(HWND w){if(auto s=surface(w))s->flat=true;InvalidateRect(w,nullptr,FALSE);}
void toggle(HWND w){if(auto s=surface(w))s->toggle=true;SendMessageW(w,WM_SETFONT,(WPARAM)font(12),TRUE);}
void outline(HWND w){if(auto s=surface(w))s->outline=true;InvalidateRect(w,nullptr,FALSE);}
void link(HWND w){if(auto s=surface(w)){s->flat=true;s->link=true;}InvalidateRect(w,nullptr,FALSE);}
void navigation(const DRAWITEMSTRUCT& item){
    if(item.itemID==(UINT)-1)return;bool selected=item.itemState&ODS_SELECTED;HBRUSH brush=CreateSolidBrush(selected?RGB(228,234,252):RGB(246,248,253));FillRect(item.hDC,&item.rcItem,brush);DeleteObject(brush);if(selected){RECT stripe=item.rcItem;stripe.right=stripe.left+3;stripe.top+=12;stripe.bottom-=12;brush=CreateSolidBrush(RGB(89,110,222));FillRect(item.hDC,&stripe,brush);DeleteObject(brush);}int length=(int)SendMessageW(item.hwndItem,LB_GETTEXTLEN,item.itemID,0);std::wstring label(length+1,0);SendMessageW(item.hwndItem,LB_GETTEXT,item.itemID,(LPARAM)label.data());label.resize(length);RECT r=item.rcItem;r.left+=16;text(item.hDC,utf8(label),r,font(13.333f),selected?RGB(63,82,185):RGB(81,96,131));
}
LRESULT colors(UINT message,WPARAM value,LPARAM control){HDC dc=(HDC)value;SetBkMode(dc,TRANSPARENT);SetTextColor(dc,RGB(61,77,115));SetBkColor(dc,RGB(255,255,255));if(message==WM_CTLCOLORLISTBOX&&GetDlgCtrlID((HWND)control)==900){static HBRUSH pale=CreateSolidBrush(RGB(246,248,253));SetBkColor(dc,RGB(246,248,253));return(LRESULT)pale;}return(LRESULT)GetStockObject(WHITE_BRUSH);}
}
