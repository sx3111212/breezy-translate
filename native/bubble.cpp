#include "app.hpp"

namespace breezy {
void App::render_bubble(const Path& preview){
    using namespace Gdiplus;if(!bubble_||!image_)return;constexpr int width=196,height=48;
    HDC screen=GetDC(nullptr),memory=CreateCompatibleDC(screen);BITMAPINFO info{};info.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);info.bmiHeader.biWidth=width;info.bmiHeader.biHeight=-height;info.bmiHeader.biPlanes=1;info.bmiHeader.biBitCount=32;info.bmiHeader.biCompression=BI_RGB;void* pixels=nullptr;HBITMAP bitmap=CreateDIBSection(screen,&info,DIB_RGB_COLORS,&pixels,nullptr,0);auto old=SelectObject(memory,bitmap);memset(pixels,0,width*height*4);
    {Bitmap surface(width,height,width*4,PixelFormat32bppPARGB,(BYTE*)pixels);Graphics g(&surface);g.SetSmoothingMode(SmoothingModeAntiAlias);g.SetInterpolationMode(InterpolationModeHighQualityBicubic);g.SetPixelOffsetMode(PixelOffsetModeHighQuality);
        if(reveal_>0){float slide=(1-reveal_)*15;g.TranslateTransform(slide,0);theme::rounded(g,RectF(expand_right_?23:8,10,169,32),15,Color((BYTE)(20*reveal_),35,45,80));theme::rounded(g,RectF(expand_right_?21:6,8,169,32),15,Color((BYTE)(248*reveal_),250,251,255),Color((BYTE)(180*reveal_),219,224,243));
            auto prefs=preferences.snapshot();auto caption=busy_?t("translate")+"…":anchor_.selection?t("readKey"):t("translate")+" → "+strings.language_name(get(prefs,"InputTarget","en"));
            Font title(L"Microsoft YaHei UI",busy_?9.f:8.5f);SolidBrush ink(Color((BYTE)(255*reveal_),61,64,100));StringFormat center;center.SetAlignment(StringAlignmentCenter);center.SetLineAlignment(StringAlignmentCenter);center.SetTrimming(StringTrimmingEllipsisCharacter);center.SetFormatFlags(StringFormatFlagsNoWrap);
            g.DrawString(wide(caption).c_str(),-1,&title,RectF(expand_right_?50:12,busy_?8:7,134,busy_?32:18),&center,&ink);
            if(!busy_){bool enabled=flag(prefs,"HotkeysEnabled",true)&&flag(prefs,anchor_.selection?"ReadHotkeyEnabled":"InputHotkeyEnabled",true);auto shortcut=enabled?get(prefs,anchor_.selection?"ReadHotkey":"InputHotkey"):t("shortcutDisabled");Font small(L"Microsoft YaHei UI",7.5f);SolidBrush muted(Color((BYTE)(255*reveal_),102,111,145));g.DrawString(wide(shortcut).c_str(),-1,&small,RectF(expand_right_?50:12,24,134,15),&center,&muted);}g.TranslateTransform(-slide,0);
        }
        float size=41+reveal_*3,center=expand_right_?24:172;g.DrawImage(image_.get(),RectF(center-size/2,24-size/2-reveal_,size,size));
        if(busy_){Pen ring(Color(210,120,106,235),2.5f);ring.SetStartCap(LineCapRound);ring.SetEndCap(LineCapRound);g.DrawArc(&ring,center-17,6.f,34.f,34.f,phase_,110.f);}
        if(success_until_>GetTickCount()){SolidBrush fill(Color(36,174,139));g.FillEllipse(&fill,center+9,30.f,13.f,13.f);Pen tick(Color::White,1.8f);tick.SetStartCap(LineCapRound);tick.SetEndCap(LineCapRound);g.DrawLine(&tick,center+12,36.f,center+15,39.f);g.DrawLine(&tick,center+15,39.f,center+19,34.f);}
        if(!preview.empty()){g.Flush(FlushIntentionSync);UINT count=0,size=0;GetImageEncodersSize(&count,&size);std::vector<BYTE> data(size);auto codecs=(ImageCodecInfo*)data.data();GetImageEncoders(count,size,codecs);for(UINT i=0;i<count;i++)if(wcscmp(codecs[i].MimeType,L"image/png")==0){surface.Save(preview.c_str(),&codecs[i].Clsid,nullptr);break;}}
    }
    SIZE extent{width,height};POINT source{};BLENDFUNCTION blend{AC_SRC_OVER,0,255,AC_SRC_ALPHA};UpdateLayeredWindow(bubble_,screen,nullptr,&extent,memory,&source,0,&blend,ULW_ALPHA);SelectObject(memory,old);DeleteObject(bitmap);DeleteDC(memory);ReleaseDC(nullptr,screen);
}
}
