#pragma once
#include "common.hpp"
#include <gdiplus.h>

namespace breezy::theme {
HFONT font(float pixels=12,bool bold=false,const wchar_t* family=L"Microsoft YaHei UI");
void release_fonts();
void rounded(Gdiplus::Graphics& graphics,Gdiplus::RectF rectangle,float radius,Gdiplus::Color fill,Gdiplus::Color border=Gdiplus::Color(0,0,0,0));
void text(HDC dc,const std::string& value,RECT rectangle,HFONT font,COLORREF color,UINT flags=DT_LEFT|DT_VCENTER|DT_SINGLELINE|DT_END_ELLIPSIS);
void speaker(Gdiplus::Graphics& graphics,float x,float y,bool playing=false);
void style(HWND window,const wchar_t* type);
void flat(HWND button);
void toggle(HWND button);
void outline(HWND button);
void link(HWND button);
void navigation(const DRAWITEMSTRUCT& item);
LRESULT colors(UINT message,WPARAM dc,LPARAM control);
}
