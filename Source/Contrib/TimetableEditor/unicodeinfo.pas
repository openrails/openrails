unit unicodeinfo;

(*
 * FreePascal translation of the utf8proc library plus some additions: 2008 Theo Lustenberger
 * Original license of the C version read below.
 * See http://www.flexiguided.de/publications.utf8proc.en.html
 *)

(*
 *  Copyright (c) 2006-2007 Jan Behrens, FlexiGuided GmbH, Berlin
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 *)

(*
 *  This library contains derived data from a modified version of the
 *  Unicode data files.
 *
 *  The original data files are available at
 *  http://www.unicode.org/Public/UNIDATA/
 *
 *  Please notice the copyright statement in the file "uniinfo_data.inc".
 *)

{$IFDEF fpc}
{$MODE objfpc}{$H+}
{$ENDIF}

interface

uses
  Classes, SysUtils, LazUTF8;

const
  UTF8PROC_NULLTERM = 1 shl 0;
  UTF8PROC_STABLE = 1 shl 1;
  UTF8PROC_COMPAT = 1 shl 2;
  UTF8PROC_COMPOSE = 1 shl 3;
  UTF8PROC_DECOMPOSE = 1 shl 4;
  UTF8PROC_IGNORE = 1 shl 5;
  UTF8PROC_REJECTNA = 1 shl 6;
  UTF8PROC_NLF2LS = 1 shl 7;
  UTF8PROC_NLF2PS = 1 shl 8;
  UTF8PROC_NLF2LF = UTF8PROC_NLF2LS or UTF8PROC_NLF2PS;
  UTF8PROC_STRIPCC = 1 shl 9;
  UTF8PROC_CASEFOLD = 1 shl 10;
  UTF8PROC_CHARBOUND = 1 shl 11;
  UTF8PROC_LUMP = 1 shl 12;
  UTF8PROC_STRIPMARK = 1 shl 13;

  UTF8PROC_HANGUL_SBASE = $AC00;
  UTF8PROC_HANGUL_LBASE = $1100;
  UTF8PROC_HANGUL_VBASE = $1161;
  UTF8PROC_HANGUL_TBASE = $11A7;
  UTF8PROC_HANGUL_LCOUNT = 19;
  UTF8PROC_HANGUL_VCOUNT = 21;
  UTF8PROC_HANGUL_TCOUNT = 28;
  UTF8PROC_HANGUL_NCOUNT = 588;
  UTF8PROC_HANGUL_SCOUNT = 11172;
  UTF8PROC_HANGUL_L_START = $1100;
  UTF8PROC_HANGUL_L_END = $115A;
  UTF8PROC_HANGUL_L_FILLER = $115F;
  UTF8PROC_HANGUL_V_START = $1160;
  UTF8PROC_HANGUL_V_END = $11A3;
  UTF8PROC_HANGUL_T_START = $11A8;
  UTF8PROC_HANGUL_T_END = $11FA;
  UTF8PROC_HANGUL_S_START = $AC00;
  UTF8PROC_HANGUL_S_END = $D7A4;

  UTF8PROC_BOUNDCLASS_START = 0;
  UTF8PROC_BOUNDCLASS_OTHER = 1;
  UTF8PROC_BOUNDCLASS_CR = 2;
  UTF8PROC_BOUNDCLASS_LF = 3;
  UTF8PROC_BOUNDCLASS_CONTROL = 4;
  UTF8PROC_BOUNDCLASS_EXTEND = 5;
  UTF8PROC_BOUNDCLASS_L = 6;
  UTF8PROC_BOUNDCLASS_V = 7;
  UTF8PROC_BOUNDCLASS_T = 8;
  UTF8PROC_BOUNDCLASS_LV = 9;
  UTF8PROC_BOUNDCLASS_LVT = 10;

  UTF8PROC_CATEGORY_LU = 1;
  UTF8PROC_CATEGORY_LL = 2;
  UTF8PROC_CATEGORY_LT = 3;
  UTF8PROC_CATEGORY_LM = 4;
  UTF8PROC_CATEGORY_LO = 5;
  UTF8PROC_CATEGORY_MN = 6;
  UTF8PROC_CATEGORY_MC = 7;
  UTF8PROC_CATEGORY_ME = 8;
  UTF8PROC_CATEGORY_ND = 9;
  UTF8PROC_CATEGORY_NL = 10;
  UTF8PROC_CATEGORY_NO = 11;
  UTF8PROC_CATEGORY_PC = 12;
  UTF8PROC_CATEGORY_PD = 13;
  UTF8PROC_CATEGORY_PS = 14;
  UTF8PROC_CATEGORY_PE = 15;
  UTF8PROC_CATEGORY_PI = 16;
  UTF8PROC_CATEGORY_PF = 17;
  UTF8PROC_CATEGORY_PO = 18;
  UTF8PROC_CATEGORY_SM = 19;
  UTF8PROC_CATEGORY_SC = 20;
  UTF8PROC_CATEGORY_SK = 21;
  UTF8PROC_CATEGORY_SO = 22;
  UTF8PROC_CATEGORY_ZS = 23;
  UTF8PROC_CATEGORY_ZL = 24;
  UTF8PROC_CATEGORY_ZP = 25;
  UTF8PROC_CATEGORY_CC = 26;
  UTF8PROC_CATEGORY_CF = 27;
  UTF8PROC_CATEGORY_CS = 28;
  UTF8PROC_CATEGORY_CO = 29;
  UTF8PROC_CATEGORY_CN = 30;

  UTF8PROC_BIDI_CLASS_L = 1;
  UTF8PROC_BIDI_CLASS_LRE = 2;
  UTF8PROC_BIDI_CLASS_LRO = 3;
  UTF8PROC_BIDI_CLASS_R = 4;
  UTF8PROC_BIDI_CLASS_AL = 5;
  UTF8PROC_BIDI_CLASS_RLE = 6;
  UTF8PROC_BIDI_CLASS_RLO = 7;
  UTF8PROC_BIDI_CLASS_PDF = 8;
  UTF8PROC_BIDI_CLASS_EN = 9;
  UTF8PROC_BIDI_CLASS_ES = 10;
  UTF8PROC_BIDI_CLASS_ET = 11;
  UTF8PROC_BIDI_CLASS_AN = 12;
  UTF8PROC_BIDI_CLASS_CS = 13;
  UTF8PROC_BIDI_CLASS_NSM = 14;
  UTF8PROC_BIDI_CLASS_BN = 15;
  UTF8PROC_BIDI_CLASS_B = 16;
  UTF8PROC_BIDI_CLASS_S = 17;
  UTF8PROC_BIDI_CLASS_WS = 18;
  UTF8PROC_BIDI_CLASS_ON = 19;

  UTF8PROC_DECOMP_TYPE_FONT = 1;
  UTF8PROC_DECOMP_TYPE_NOBREAK = 2;
  UTF8PROC_DECOMP_TYPE_INITIAL = 3;
  UTF8PROC_DECOMP_TYPE_MEDIAL = 4;
  UTF8PROC_DECOMP_TYPE_FINAL = 5;
  UTF8PROC_DECOMP_TYPE_ISOLATED = 6;
  UTF8PROC_DECOMP_TYPE_CIRCLE = 7;
  UTF8PROC_DECOMP_TYPE_SUPER = 8;
  UTF8PROC_DECOMP_TYPE_SUB = 9;
  UTF8PROC_DECOMP_TYPE_VERTICAL = 10;
  UTF8PROC_DECOMP_TYPE_WIDE = 11;
  UTF8PROC_DECOMP_TYPE_NARROW = 12;
  UTF8PROC_DECOMP_TYPE_SMALL = 13;
  UTF8PROC_DECOMP_TYPE_SQUARE = 14;
  UTF8PROC_DECOMP_TYPE_FRACTION = 15;
  UTF8PROC_DECOMP_TYPE_COMPAT = 16;

  UTF8PROC_ERROR_NOMEM = -(1);
  UTF8PROC_ERROR_OVERFLOW = -(2);
  UTF8PROC_ERROR_INVALIDUTF8 = -(3);
  UTF8PROC_ERROR_NOTASSIGNED = -(4);
  UTF8PROC_ERROR_INVALIDOPTS = -(5);

  CategoryStrings: array[1..30] of ShortString = (
    ('Letter, Uppercase'),
    ('Letter, Lowercase'),
    ('Letter, Titlecase'),
    ('Letter, Modifier'),
    ('Letter, Other'),
    ('Mark, Nonspacing'),
    ('Mark, Spacing Combining'),
    ('Mark, Enclosing'),
    ('Number, Decimal Digit'),
    ('Number, Letter'),
    ('Number, Other'),
    ('Punctuation, Connector'),
    ('Punctuation, Dash'),
    ('Punctuation, Open'),
    ('Punctuation, Close'),
    ('Punctuation, Initial quote (may behave like Ps or Pe depending on usage)'),
    ('Punctuation, Final quote (may behave like Ps or Pe depending on usage)'),
    ('Punctuation, Other'),
    ('Symbol, Math'),
    ('Symbol, Currency'),
    ('Symbol, Modifier'),
    ('Symbol, Other'),
    ('Separator, Space'),
    ('Separator, Line'),
    ('Separator, Paragraph'),
    ('Other, Control'),
    ('Other, Format'),
    ('Other, Surrogate'),
    ('Other, Private Use'),
    ('Other, Not Assigned (no characters in the file have this property)')
    );

  BIDIStrings: array[1..19] of ShortString = (
    ('Left-to-Right'),
    ('Left-to-Right Embedding'),
    ('Left-to-Right Override'),
    ('Right-to-Left'),
    ('Right-to-Left Arabic'),
    ('Right-to-Left Embedding'),
    ('Right-to-Left Override'),
    ('Pop Directional Format'),
    ('European Number'),
    ('European Number Separator'),
    ('European Number Terminator'),
    ('Arabic Number'),
    ('Common Number Separator'),
    ('Non-Spacing Mark'),
    ('Boundary Neutral'),
    ('Paragraph Separator'),
    ('Segment Separator'),
    ('Whitespace'),
    ('Other Neutrals')
    );

  DecompStrings: array[1..16] of ShortString = (
    ('A font variant (e.g. a blackletter form).'),
    ('A no-break version of a space or hyphen.'),
    ('An initial presentation form (Arabic).'),
    ('A medial presentation form (Arabic).'),
    ('A final presentation form (Arabic).'),
    ('An isolated presentation form (Arabic).'),
    ('An encircled form.'),
    ('A superscript form.'),
    ('A subscript form.'),
    ('A vertical layout presentation form.'),
    ('A wide (or zenkaku) compatibility character.'),
    ('A narrow (or hankaku) compatibility character.'),
    ('A small variant form (CNS compatibility).'),
    ('A CJK squared font variant.'),
    ('A vulgar fraction form.'),
    ('Otherwise unspecified compatibility character.')
    );

  SSIZE_MAX = (High(longword) - 1) div 2;

  utf8proc_utf8class: array[0..Pred(256)] of Shortint =
  (1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
    2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
    3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
    4, 4, 4, 4, 4, 4, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0);

type TUnicodeRange = record
    S: longint;
    E: longint;
    PG: string[50];
  end;

const
  MaxUnicodeRanges = 153;
  UnicodeRanges: array[0..MaxUnicodeRanges] of TUnicodeRange = (

    (S: $0000; E: $007F; PG: 'Basic Latin'),
    (S: $0080; E: $00FF; PG: 'Latin-1 Supplement'),
    (S: $0100; E: $017F; PG: 'Latin Extended-A'),
    (S: $0180; E: $024F; PG: 'Latin Extended-B'),
    (S: $0250; E: $02AF; PG: 'IPA Extensions'),
    (S: $02B0; E: $02FF; PG: 'Spacing Modifier Letters'),
    (S: $0300; E: $036F; PG: 'Combining Diacritical Marks'),
    (S: $0370; E: $03FF; PG: 'Greek and Coptic'),
    (S: $0400; E: $04FF; PG: 'Cyrillic'),
    (S: $0500; E: $052F; PG: 'Cyrillic Supplement'),
    (S: $0530; E: $058F; PG: 'Armenian'),
    (S: $0590; E: $05FF; PG: 'Hebrew'),
    (S: $0600; E: $06FF; PG: 'Arabic'),
    (S: $0700; E: $074F; PG: 'Syriac'),
    (S: $0750; E: $077F; PG: 'Arabic Supplement'),
    (S: $0780; E: $07BF; PG: 'Thaana'),
    (S: $07C0; E: $07FF; PG: 'NKo'),
    (S: $0900; E: $097F; PG: 'Devanagari'),
    (S: $0980; E: $09FF; PG: 'Bengali'),
    (S: $0A00; E: $0A7F; PG: 'Gurmukhi'),
    (S: $0A80; E: $0AFF; PG: 'Gujarati'),
    (S: $0B00; E: $0B7F; PG: 'Oriya'),
    (S: $0B80; E: $0BFF; PG: 'Tamil'),
    (S: $0C00; E: $0C7F; PG: 'Telugu'),
    (S: $0C80; E: $0CFF; PG: 'Kannada'),
    (S: $0D00; E: $0D7F; PG: 'Malayalam'),
    (S: $0D80; E: $0DFF; PG: 'Sinhala'),
    (S: $0E00; E: $0E7F; PG: 'Thai'),
    (S: $0E80; E: $0EFF; PG: 'Lao'),
    (S: $0F00; E: $0FFF; PG: 'Tibetan'),
    (S: $1000; E: $109F; PG: 'Myanmar'),
    (S: $10A0; E: $10FF; PG: 'Georgian'),
    (S: $1100; E: $11FF; PG: 'Hangul Jamo'),
    (S: $1200; E: $137F; PG: 'Ethiopic'),
    (S: $1380; E: $139F; PG: 'Ethiopic Supplement'),
    (S: $13A0; E: $13FF; PG: 'Cherokee'),
    (S: $1400; E: $167F; PG: 'Unified Canadian Aboriginal Syllabics'),
    (S: $1680; E: $169F; PG: 'Ogham'),
    (S: $16A0; E: $16FF; PG: 'Runic'),
    (S: $1700; E: $171F; PG: 'Tagalog'),
    (S: $1720; E: $173F; PG: 'Hanunoo'),
    (S: $1740; E: $175F; PG: 'Buhid'),
    (S: $1760; E: $177F; PG: 'Tagbanwa'),
    (S: $1780; E: $17FF; PG: 'Khmer'),
    (S: $1800; E: $18AF; PG: 'Mongolian'),
    (S: $1900; E: $194F; PG: 'Limbu'),
    (S: $1950; E: $197F; PG: 'Tai Le'),
    (S: $1980; E: $19DF; PG: 'New Tai Lue'),
    (S: $19E0; E: $19FF; PG: 'Khmer Symbols'),
    (S: $1A00; E: $1A1F; PG: 'Buginese'),
    (S: $1B00; E: $1B7F; PG: 'Balinese'),
    (S: $1D00; E: $1D7F; PG: 'Phonetic Extensions'),
    (S: $1D80; E: $1DBF; PG: 'Phonetic Extensions Supplement'),
    (S: $1DC0; E: $1DFF; PG: 'Combining Diacritical Marks Supplement'),
    (S: $1E00; E: $1EFF; PG: 'Latin Extended Additional'),
    (S: $1F00; E: $1FFF; PG: 'Greek Extended'),
    (S: $2000; E: $206F; PG: 'General Punctuation'),
    (S: $2070; E: $209F; PG: 'Superscripts and Subscripts'),
    (S: $20A0; E: $20CF; PG: 'Currency Symbols'),
    (S: $20D0; E: $20FF; PG: 'Combining Diacritical Marks for Symbols'),
    (S: $2100; E: $214F; PG: 'Letterlike Symbols'),
    (S: $2150; E: $218F; PG: 'Number Forms'),
    (S: $2190; E: $21FF; PG: 'Arrows'),
    (S: $2200; E: $22FF; PG: 'Mathematical Operators'),
    (S: $2300; E: $23FF; PG: 'Miscellaneous Technical'),
    (S: $2400; E: $243F; PG: 'Control Pictures'),
    (S: $2440; E: $245F; PG: 'Optical Character Recognition'),
    (S: $2460; E: $24FF; PG: 'Enclosed Alphanumerics'),
    (S: $2500; E: $257F; PG: 'Box Drawing'),
    (S: $2580; E: $259F; PG: 'Block Elements'),
    (S: $25A0; E: $25FF; PG: 'Geometric Shapes'),
    (S: $2600; E: $26FF; PG: 'Miscellaneous Symbols'),
    (S: $2700; E: $27BF; PG: 'Dingbats'),
    (S: $27C0; E: $27EF; PG: 'Miscellaneous Mathematical Symbols-A'),
    (S: $27F0; E: $27FF; PG: 'Supplemental Arrows-A'),
    (S: $2800; E: $28FF; PG: 'Braille Patterns'),
    (S: $2900; E: $297F; PG: 'Supplemental Arrows-B'),
    (S: $2980; E: $29FF; PG: 'Miscellaneous Mathematical Symbols-B'),
    (S: $2A00; E: $2AFF; PG: 'Supplemental Mathematical Operators'),
    (S: $2B00; E: $2BFF; PG: 'Miscellaneous Symbols and Arrows'),
    (S: $2C00; E: $2C5F; PG: 'Glagolitic'),
    (S: $2C60; E: $2C7F; PG: 'Latin Extended-C'),
    (S: $2C80; E: $2CFF; PG: 'Coptic'),
    (S: $2D00; E: $2D2F; PG: 'Georgian Supplement'),
    (S: $2D30; E: $2D7F; PG: 'Tifinagh'),
    (S: $2D80; E: $2DDF; PG: 'Ethiopic Extended'),
    (S: $2E00; E: $2E7F; PG: 'Supplemental Punctuation'),
    (S: $2E80; E: $2EFF; PG: 'CJK Radicals Supplement'),
    (S: $2F00; E: $2FDF; PG: 'Kangxi Radicals'),
    (S: $2FF0; E: $2FFF; PG: 'Ideographic Description Characters'),
    (S: $3000; E: $303F; PG: 'CJK Symbols and Punctuation'),
    (S: $3040; E: $309F; PG: 'Hiragana'),
    (S: $30A0; E: $30FF; PG: 'Katakana'),
    (S: $3100; E: $312F; PG: 'Bopomofo'),
    (S: $3130; E: $318F; PG: 'Hangul Compatibility Jamo'),
    (S: $3190; E: $319F; PG: 'Kanbun'),
    (S: $31A0; E: $31BF; PG: 'Bopomofo Extended'),
    (S: $31C0; E: $31EF; PG: 'CJK Strokes'),
    (S: $31F0; E: $31FF; PG: 'Katakana Phonetic Extensions'),
    (S: $3200; E: $32FF; PG: 'Enclosed CJK Letters and Months'),
    (S: $3300; E: $33FF; PG: 'CJK Compatibility'),
    (S: $3400; E: $4DBF; PG: 'CJK Unified Ideographs Extension A'),
    (S: $4DC0; E: $4DFF; PG: 'Yijing Hexagram Symbols'),
    (S: $4E00; E: $9FFF; PG: 'CJK Unified Ideographs'),
    (S: $A000; E: $A48F; PG: 'Yi Syllables'),
    (S: $A490; E: $A4CF; PG: 'Yi Radicals'),
    (S: $A700; E: $A71F; PG: 'Modifier Tone Letters'),
    (S: $A720; E: $A7FF; PG: 'Latin Extended-D'),
    (S: $A800; E: $A82F; PG: 'Syloti Nagri'),
    (S: $A840; E: $A87F; PG: 'Phags-pa'),
    (S: $AC00; E: $D7AF; PG: 'Hangul Syllables'),
    (S: $D800; E: $DB7F; PG: 'High Surrogates'),
    (S: $DB80; E: $DBFF; PG: 'High Private Use Surrogates'),
    (S: $DC00; E: $DFFF; PG: 'Low Surrogates'),
    (S: $E000; E: $F8FF; PG: 'Private Use Area'),
    (S: $F900; E: $FAFF; PG: 'CJK Compatibility Ideographs'),
    (S: $FB00; E: $FB4F; PG: 'Alphabetic Presentation Forms'),
    (S: $FB50; E: $FDFF; PG: 'Arabic Presentation Forms-A'),
    (S: $FE00; E: $FE0F; PG: 'Variation Selectors'),
    (S: $FE10; E: $FE1F; PG: 'Vertical Forms'),
    (S: $FE20; E: $FE2F; PG: 'Combining Half Marks'),
    (S: $FE30; E: $FE4F; PG: 'CJK Compatibility Forms'),
    (S: $FE50; E: $FE6F; PG: 'Small Form Variants'),
    (S: $FE70; E: $FEFF; PG: 'Arabic Presentation Forms-B'),
    (S: $FF00; E: $FFEF; PG: 'Halfwidth and Fullwidth Forms'),
    (S: $FFF0; E: $FFFF; PG: 'Specials'),
    (S: $10000; E: $1007F; PG: 'Linear B Syllabary'),
    (S: $10080; E: $100FF; PG: 'Linear B Ideograms'),
    (S: $10100; E: $1013F; PG: 'Aegean Numbers'),
    (S: $10140; E: $1018F; PG: 'Ancient Greek Numbers'),
    (S: $10300; E: $1032F; PG: 'Old Italic'),
    (S: $10330; E: $1034F; PG: 'Gothic'),
    (S: $10380; E: $1039F; PG: 'Ugaritic'),
    (S: $103A0; E: $103DF; PG: 'Old Persian'),
    (S: $10400; E: $1044F; PG: 'Deseret'),
    (S: $10450; E: $1047F; PG: 'Shavian'),
    (S: $10480; E: $104AF; PG: 'Osmanya'),
    (S: $10800; E: $1083F; PG: 'Cypriot Syllabary'),
    (S: $10900; E: $1091F; PG: 'Phoenician'),
    (S: $10A00; E: $10A5F; PG: 'Kharoshthi'),
    (S: $12000; E: $123FF; PG: 'Cuneiform'),
    (S: $12400; E: $1247F; PG: 'Cuneiform Numbers and Punctuation'),
    (S: $1D000; E: $1D0FF; PG: 'Byzantine Musical Symbols'),
    (S: $1D100; E: $1D1FF; PG: 'Musical Symbols'),
    (S: $1D200; E: $1D24F; PG: 'Ancient Greek Musical Notation'),
    (S: $1D300; E: $1D35F; PG: 'Tai Xuan Jing Symbols'),
    (S: $1D360; E: $1D37F; PG: 'Counting Rod Numerals'),
    (S: $1D400; E: $1D7FF; PG: 'Mathematical Alphanumeric Symbols'),
    (S: $20000; E: $2A6DF; PG: 'CJK Unified Ideographs Extension B'),
    (S: $2F800; E: $2FA1F; PG: 'CJK Compatibility Ideographs Supplement'),
    (S: $E0000; E: $E007F; PG: 'Tags'),
    (S: $E0100; E: $E01EF; PG: 'Variation Selectors Supplement'),
    (S: $F0000; E: $FFFFF; PG: 'Supplementary Private Use Area-A'),
    (S: $100000; E: $10FFFF; PG: 'Supplementary Private Use Area-B')
    );

type

  PPByte = ^PByte;

  utf8proc_propval_t = Smallint;
  utf8proc_property_t = record
    category: utf8proc_propval_t;
    combining_class: utf8proc_propval_t;
    bidi_class: utf8proc_propval_t;
    decomp_type: utf8proc_propval_t;
    decomp_mapping: Plongint;
    bidi_mirrored: boolean;
    uppercase_mapping: longint;
    lowercase_mapping: longint;
    titlecase_mapping: longint;
    comb1st_index: longint;
    comb2nd_index: longint;
    comp_exclusion: boolean;
    ignorable: boolean;
    control_boundary: boolean;
    extend: boolean;
    casefold_mapping: Plongint;
  end;
  putf8proc_property_t = ^utf8proc_property_t;

function utf8proc_NFD(str: PChar): PChar;
function utf8proc_NFC(str: PChar): PChar;
function utf8proc_NFKD(str: PChar): PChar;
function utf8proc_NFKC(str: PChar): PChar;
function utf8proc_codepoint_valid(uc: longint): boolean;
function utf8proc_iterate(str: PByte; strlen: longint; dst: pLongInt): longint;
function utf8proc_encode_char(uc: longint; dst: PByte): longint;
function utf8proc_get_property(uc: longint): putf8proc_property_t;
function utf8proc_decompose_char(uc: longint; dst: plongint; bufsize: longint; options: integer; last_boundclass: pinteger): longint;
function utf8proc_decomposer(str: PByte; strlen: longint; buffer: plongint; bufsize: longint; options: integer): longint;
function utf8proc_map(str: PByte; strlen: longint; dstptr: PPByte; options: integer): longint;
function utf8proc_reencode(buffer: plongint; length: longint; options: integer): longint;
function utf8proc_getinfostring(pr: putf8proc_property_t; Chara: Longint = -1): string;

implementation

{$I uniinfo_data.inc}

function utf8proc_errmsg(errcode: longint): pchar;
begin
  case errcode of
    UTF8PROC_ERROR_NOMEM:
      result := 'Memory for processing UTF-8 data could not be allocated.';
    UTF8PROC_ERROR_OVERFLOW:
      result := 'UTF-8 string is too long to be processed.';
    UTF8PROC_ERROR_INVALIDUTF8:
      result := 'Invalid UTF-8 string';
    UTF8PROC_ERROR_NOTASSIGNED:
      result := 'Unassigned Unicode code point found in UTF-8 string.';
    UTF8PROC_ERROR_INVALIDOPTS:
      result := 'Invalid options for UTF-8 processing chosen.';
  else
    result := 'An unknown error occured while processing UTF-8 data.';
  end;
end;

function utf8proc_codepoint_valid(uc: longint): boolean;
begin
  if ((uc < 0) or (uc >= $110000) or
    (((uc and $FFFF) >= $FFFE)) or ((uc >= $D800) and (uc < $E000)) or
    ((uc >= $FDD0) and (uc < $FDF0))) then result := false else result := true;
end;

function utf8proc_iterate(str: PByte; strlen: longint; dst: pLongInt): longint;
var
  length: integer;
  i: integer;
  uc: longint;
begin
  uc := -1;
  dst^ := -1;
  if strlen = 0 then
  begin
    result := 0;
    exit;
  end;
  length := utf8proc_utf8class[(str[0])];
  if length = 0 then
  begin
    result := UTF8PROC_ERROR_INVALIDUTF8;
    exit;
  end;
  if ((strlen >= 0) and (length > strlen)) then
  begin
    result := UTF8PROC_ERROR_INVALIDUTF8;
    exit;
  end;
  for i := 1 to Pred(length) do
  begin
    if ((str[i]) and $C0) <> $80 then
    begin
      result := UTF8PROC_ERROR_INVALIDUTF8;
      exit;
    end;
  end;
  case length of
    1:
      begin
        uc := (str[0]);
      end;
    2:
      begin
        uc := (((str[0]) and $1F) shl 6) + ((str[1]) and $3F);
        if uc < $80 then uc := -1;
      end;
    3:
      begin
        uc := (((str[0]) and $0F) shl 12) + (((str[1]) and $3F) shl 6) + ((str[2]) and $3F);
        if (uc < $800) or ((uc >= $D800) and (uc < $E000)) or ((uc >= $FDD0) and (uc < $FDF0)) then uc := -1;
      end;
    4:
      begin
        uc := (((str[0]) and $07) shl 18) + (((str[1]) and $3F) shl 12) + (((str[2]) and $3F) shl 6) + ((str[3]) and $3F);
        if (uc < $10000) or (uc >= $110000) then uc := -1;
      end;
  end;
  if (uc < 0) or ((uc and $FFFF) >= $FFFE) then
  begin
    result := UTF8PROC_ERROR_INVALIDUTF8;
    exit;
  end;
  dst^ := uc;
  result := length;
end;

function utf8proc_encode_char(uc: longint; dst: PByte): longint;
begin
  if uc < $00 then
  begin
    result := 0;
    exit;
  end else
    if uc < $80 then
    begin
      dst[0] := (uc);
      begin
        result := 1;
        exit;
      end;
    end else
      if uc < $800 then
      begin
        dst[0] := ($C0 + (uc shr 6));
        dst[1] := ($80 + (uc and $3F));
        begin
          result := 2;
          exit;
        end;
      end else
        if uc = $FFFF then
        begin
          dst[0] := ($FF);
          begin
            result := 1;
            exit;
          end;
        end else
          if uc = $FFFE then
          begin
            dst[0] := ($FE);
            begin
              result := 1;
              exit;
            end;
          end else
            if uc < $10000 then
            begin
              dst[0] := ($E0 + (uc shr 12));
              dst[1] := ($80 + ((uc shr 6) and $3F));
              dst[2] := ($80 + (uc and $3F));
              begin
                result := 3;
                exit;
              end;
            end else
              if uc < $110000 then
              begin
                dst[0] := ($F0 + (uc shr 18));
                dst[1] := ($80 + ((uc shr 12) and $3F));
                dst[2] := ($80 + ((uc shr 6) and $3F));
                dst[3] := ($80 + (uc and $3F));
                begin
                  result := 4;
                  exit;
                end;
              end else
              begin
                result := 0;
                exit;
              end;
end;

function utf8proc_get_property(uc: longint): putf8proc_property_t;
begin
  Result := @utf8proc_properties[utf8proc_stage2table[utf8proc_stage1table[uc shr 8] + (uc and $FF)]]
end;

function utf8proc_decompose_char(uc: longint; dst: plongint; bufsize: longint; options: integer; last_boundclass: pinteger): longint;
var aproperty: putf8proc_property_t;
  casefold_entry: plongint;
  decomp_entry: plongint;
  category: utf8proc_propval_t;
  hangul_sindex: longint;
  hangul_tindex: longint;
  written: longint;
  temp: longint;
  tbc, lbc: integer;
  boundary: boolean;

  function utf8proc_decompose_lump(replacement_uc: integer): integer;
  begin
    result := utf8proc_decompose_char(replacement_uc, dst, bufsize, options, last_boundclass);
  end;

begin
  aproperty := utf8proc_get_property(uc);
  category := aproperty^.category;
  hangul_sindex := uc - UTF8PROC_HANGUL_SBASE;

  if (options and (UTF8PROC_COMPOSE or UTF8PROC_DECOMPOSE)) <> 0 then
  begin
    if (hangul_sindex >= 0) and (hangul_sindex < UTF8PROC_HANGUL_SCOUNT) then
    begin
      if bufsize >= 1
        then
      begin
        dst[0] := UTF8PROC_HANGUL_LBASE + hangul_sindex div UTF8PROC_HANGUL_NCOUNT;
        if bufsize >= 2 then
          dst[1] := UTF8PROC_HANGUL_VBASE + (hangul_sindex mod UTF8PROC_HANGUL_NCOUNT) div UTF8PROC_HANGUL_TCOUNT;
      end;
      hangul_tindex := hangul_sindex mod UTF8PROC_HANGUL_TCOUNT;
      if hangul_tindex = 0 then
      begin
        result := 2;
        exit;
      end;
      if bufsize >= 3 then
        dst[2] := UTF8PROC_HANGUL_TBASE + hangul_tindex;
      begin
        result := 3;
        exit;
      end;
    end;
  end;

  if (options and UTF8PROC_REJECTNA) <> 0
    then
  begin
    if category = 0 then
    begin
      result := UTF8PROC_ERROR_NOTASSIGNED;
      exit;
    end;
  end;
  if (options and UTF8PROC_IGNORE) <> 0
    then
  begin
    if aproperty^.ignorable then
    begin
      result := 0;
      exit;
    end;
  end;

  if (options and UTF8PROC_LUMP) <> 0
    then
  begin
    if category = UTF8PROC_CATEGORY_ZS then utf8proc_decompose_lump($0020);
    if ((uc = $2018) or (uc = $2019) or (uc = $02BC) or (uc = $02C8)) then begin result := utf8proc_decompose_lump($0027); exit; end;
    if ((category = UTF8PROC_CATEGORY_PD) or (uc = $2212)) then begin result := utf8proc_decompose_lump($002D); exit; end;
    if (uc = $2044) or (uc = $2215) then begin result := utf8proc_decompose_lump($002F); exit; end;
    if uc = $2236 then begin result := utf8proc_decompose_lump($003A); exit; end;
    if ((uc = $2039) or (uc = $2329) or (uc = $3008)) then begin result := utf8proc_decompose_lump($003C); exit; end;
    if ((uc = $203A) or (uc = $232A) or (uc = $3009)) then begin result := utf8proc_decompose_lump($003E); exit; end;
    if uc = $2216 then begin result := utf8proc_decompose_lump($005C); exit; end;
    if ((uc = $02C4) or (uc = $02C6) or (uc = $2038) or (uc = $2303)) then begin result := utf8proc_decompose_lump($005E); exit; end;
    if ((category = UTF8PROC_CATEGORY_PC) or (uc = $02CD)) then begin result := utf8proc_decompose_lump($005F); exit; end;
    if uc = $02CB then begin result := utf8proc_decompose_lump($0060); exit; end;
    if uc = $2223 then begin result := utf8proc_decompose_lump($007C); exit; end;
    if uc = $223C then begin result := utf8proc_decompose_lump($007E); exit; end;
    if ((options and UTF8PROC_NLF2LS) and (options and UTF8PROC_NLF2PS)) <> 0 then
    begin
      if ((category = UTF8PROC_CATEGORY_ZL) or (category = UTF8PROC_CATEGORY_ZP)) then begin result := utf8proc_decompose_lump($000A); exit; end;
    end;
  end;

  if (options and UTF8PROC_STRIPMARK) <> 0 then
  begin
    if ((category = UTF8PROC_CATEGORY_MN) or (category = UTF8PROC_CATEGORY_MC) or (category = UTF8PROC_CATEGORY_ME)) then
    begin
      result := 0;
      exit;
    end;
  end;

  if (options and UTF8PROC_CASEFOLD) <> 0 then
  begin
    if aproperty^.casefold_mapping <> nil then
    begin
      written := 0;
      casefold_entry := aproperty^.casefold_mapping;
      while casefold_entry^ >= 0 do
      begin
        if (bufsize > written) then temp := (bufsize - written) else temp := 0;
        written := written + utf8proc_decompose_char(casefold_entry^, dst + written, temp, options, last_boundclass);
        inc(casefold_entry);
        if (written < 0) then begin result := UTF8PROC_ERROR_OVERFLOW; exit; end;
      end;
      Result := written;
      exit;
    end;
  end;

  if (options and (UTF8PROC_COMPOSE or UTF8PROC_DECOMPOSE)) <> 0 then
  begin
    if (aproperty^.decomp_mapping <> nil) and ((aproperty^.decomp_type = 0) or (options and UTF8PROC_COMPAT <> 0)) then
    begin
      written := 0;
      decomp_entry := aproperty^.decomp_mapping;
      while decomp_entry^ >= 0 do
      begin
        if (bufsize > written) then temp := (bufsize - written) else temp := 0;
        written := written + utf8proc_decompose_char(decomp_entry^, dst + written, temp, options, last_boundclass);
        inc(decomp_entry);
        if (written < 0) then begin result := UTF8PROC_ERROR_OVERFLOW; exit; end;
      end;
      Result := written;
      exit;
    end;
  end;

  if (options and UTF8PROC_CHARBOUND) <> 0 then
  begin
    if (uc = $000D) then tbc := UTF8PROC_BOUNDCLASS_CR else
      if (uc = $000A) then tbc := UTF8PROC_BOUNDCLASS_LF else
        if (((category = UTF8PROC_CATEGORY_ZL) or (category = UTF8PROC_CATEGORY_ZP) or (category = UTF8PROC_CATEGORY_CC) or
          (category = UTF8PROC_CATEGORY_CF)) and not ((uc = $200C) or (uc = $200D))) then tbc := UTF8PROC_BOUNDCLASS_CONTROL else
          if aproperty^.extend then tbc := UTF8PROC_BOUNDCLASS_EXTEND else
            if (((uc >= UTF8PROC_HANGUL_L_START) and (uc < UTF8PROC_HANGUL_L_END)) or (uc = UTF8PROC_HANGUL_L_FILLER)) then tbc := UTF8PROC_BOUNDCLASS_L else
              if ((uc >= UTF8PROC_HANGUL_V_START) and (uc < UTF8PROC_HANGUL_V_END)) then tbc := UTF8PROC_BOUNDCLASS_V else
                if ((uc >= UTF8PROC_HANGUL_T_START) and (uc < UTF8PROC_HANGUL_T_END)) then tbc := UTF8PROC_BOUNDCLASS_T else
                  if ((uc >= UTF8PROC_HANGUL_S_START) and (uc < UTF8PROC_HANGUL_S_END)) then begin
                    if ((uc - UTF8PROC_HANGUL_SBASE) mod UTF8PROC_HANGUL_TCOUNT = 0) then
                      tbc := UTF8PROC_BOUNDCLASS_LV else tbc := UTF8PROC_BOUNDCLASS_LVT;
                  end else tbc := UTF8PROC_BOUNDCLASS_OTHER;

    lbc := last_boundclass^;

    if tbc = UTF8PROC_BOUNDCLASS_EXTEND then boundary := false else
      if lbc = UTF8PROC_BOUNDCLASS_START then boundary := true else
        if (lbc = UTF8PROC_BOUNDCLASS_CR) and (tbc = UTF8PROC_BOUNDCLASS_LF) then boundary := false else
          if lbc = UTF8PROC_BOUNDCLASS_CONTROL then boundary := true else
            if tbc = UTF8PROC_BOUNDCLASS_CONTROL then boundary := true else
              if (lbc = UTF8PROC_BOUNDCLASS_L) and
                ((tbc = UTF8PROC_BOUNDCLASS_L) or
                (tbc = UTF8PROC_BOUNDCLASS_V) or
                (tbc = UTF8PROC_BOUNDCLASS_LV) or
                (tbc = UTF8PROC_BOUNDCLASS_LVT)) then boundary := false else

                if ((lbc = UTF8PROC_BOUNDCLASS_LV) or
                  (lbc = UTF8PROC_BOUNDCLASS_V)) and
                  ((tbc = UTF8PROC_BOUNDCLASS_V) or
                  (tbc = UTF8PROC_BOUNDCLASS_T)) then boundary := false else

                  if ((lbc = UTF8PROC_BOUNDCLASS_LVT) or
                    (lbc = UTF8PROC_BOUNDCLASS_T)) and
                    (tbc = UTF8PROC_BOUNDCLASS_T) then boundary := false else boundary := true;
    last_boundclass^ := lbc;

    if boundary
      then
    begin
      if bufsize >= 1
        then
        dst[0] := $FFFF;
      if bufsize >= 2
        then
        dst[1] := uc;
      begin
        result := 2;
        exit;
      end;
    end;
  end;

  if bufsize >= 1
    then
    dst^ := uc;
  begin
    result := 1;
    exit;
  end;
end;

function utf8proc_decomposer(str: PByte; strlen: longint; buffer: plongint; bufsize: longint; options: integer): longint;
var
  property1: putf8proc_property_t;
  property2: putf8proc_property_t;
  wpos: longint;
  uc: longint;
  rpos: longint;
  decomp_result: longint;
  boundclass: integer;
  pos: longint;
  uc1: longint;
  uc2: longint;
  temp: longint;
begin
  wpos := 0;
  if (options and UTF8PROC_COMPOSE <> 0) and (options and UTF8PROC_DECOMPOSE <> 0)
    then
  begin
    result := UTF8PROC_ERROR_INVALIDOPTS;
    exit;
  end;
  if (options and UTF8PROC_STRIPMARK <> 0) and (0 = (options and UTF8PROC_COMPOSE)) and (0 = (options and UTF8PROC_DECOMPOSE))
    then
  begin
    result := UTF8PROC_ERROR_INVALIDOPTS;
    exit;
  end;
  begin

    rpos := 0;

    boundclass := UTF8PROC_BOUNDCLASS_START;
    while true do
    begin
      if (options and UTF8PROC_NULLTERM <> 0) then
      begin
        rpos := rpos + (utf8proc_iterate(str + rpos, -1, @uc));
        if uc < 0 then
        begin
          result := UTF8PROC_ERROR_INVALIDUTF8;
          exit;
        end;
        if rpos < 0 then
        begin
          result := UTF8PROC_ERROR_OVERFLOW;
          exit;
        end;
        if uc = 0 then break;
      end
      else
      begin
        if rpos >= strlen then break;
        rpos := rpos + (utf8proc_iterate(str + rpos, strlen - rpos, @uc));
        if uc < 0 then
        begin
          result := UTF8PROC_ERROR_INVALIDUTF8;
          exit;
        end;
      end;
      if (bufsize > wpos) then temp := bufsize - wpos else temp := 0;
      decomp_result := utf8proc_decompose_char(uc, buffer + wpos, temp, options, @boundclass);
      if decomp_result < 0 then
      begin
        result := decomp_result;
        exit;
      end;
      wpos := wpos + (decomp_result);
      if (wpos < 0) or (wpos > SSIZE_MAX div sizeof(longint) div 2) then
      begin
        result := UTF8PROC_ERROR_OVERFLOW;
        exit;
      end;
    end;
  end;

  if ((options and (UTF8PROC_COMPOSE or UTF8PROC_DECOMPOSE) <> 0) and (bufsize >= wpos)) then
  begin
    pos := 0;
    while pos < wpos - 1 do
    begin

      uc1 := buffer[pos];
      uc2 := buffer[pos + 1];
      property1 := utf8proc_get_property(uc1);
      property2 := utf8proc_get_property(uc2);
      if ((property1^.combining_class > property2^.combining_class) and (property2^.combining_class > 0)) then
      begin
        buffer[pos] := uc2;
        buffer[pos + 1] := uc1;
        if pos > 0 then dec(pos) else inc(pos);
      end
      else
      begin
        inc(pos);
      end;
    end;
  end;
  begin
    result := wpos;
    exit;
  end;
end;

function utf8proc_reencode(buffer: plongint; length: longint; options: integer): longint;
var
  starter_property: putf8proc_property_t;
  current_property: putf8proc_property_t;
  rpos: longint;
  wpos: longint;
  uc: longint;
  starter: plongint;
  current_char: longint;
  max_combining_class: utf8proc_propval_t;
  composition: longint;
  hangul_lindex: longint;
  hangul_sindex: longint;
  hangul_vindex: longint;
  hangul_tindex: longint;
begin
  starter_property := nil;
  if (options and (UTF8PROC_NLF2LS or UTF8PROC_NLF2PS or UTF8PROC_STRIPCC) <> 0) then
  begin
    wpos := 0;
    rpos := 0;
    while rpos < length do
    begin
      uc := buffer[rpos];
      if ((uc = $000D) and (rpos < length - 1) and (buffer[rpos + 1] = $000A)) then inc(rpos);
      if ((uc = $000A) or (uc = $000D) or (uc = $0085) or ((options and UTF8PROC_STRIPCC <> 0) and ((uc = $000B) or (uc = $000C)))) then
      begin
        if (options and UTF8PROC_NLF2LS) <> 0 then
        begin
          if (options and UTF8PROC_NLF2PS) <> 0 then
          begin
            buffer[wpos] := $000A;
            inc(wpos);
          end
          else
          begin
            buffer[wpos] := $2028;
            inc(wpos);
          end;
        end
        else
        begin
          if (options and UTF8PROC_NLF2PS) <> 0 then
          begin
            buffer[wpos] := $2029;
            inc(wpos);
          end
          else
          begin
            buffer[wpos] := $0020;
            inc(wpos);
          end;
        end;
      end
      else
        if ((options and UTF8PROC_STRIPCC <> 0) and ((uc < $0020) or ((uc >= $007F) and (uc < $00A0)))) then
        begin
          if uc = $0009 then
          begin
            buffer[wpos] := $0020;
            inc(wpos);
          end;
        end
        else
        begin
          buffer[wpos] := uc;
          inc(wpos);
        end;
    end;
    inc(rpos);
    length := wpos;
  end;
  if (options and UTF8PROC_COMPOSE) <> 0 then
  begin
    starter := nil;
    starter_property := nil;
    max_combining_class := -1;

    wpos := 0;

    for rpos := 0 to Pred(length) do
    begin
      current_char := buffer[rpos];
      current_property := utf8proc_get_property(current_char);
      if (starter <> nil) and (current_property^.combining_class > max_combining_class) then
      begin
        hangul_lindex := starter^ - UTF8PROC_HANGUL_LBASE;
        if (hangul_lindex >= 0) and (hangul_lindex < UTF8PROC_HANGUL_LCOUNT) then
        begin
          hangul_vindex := current_char - UTF8PROC_HANGUL_VBASE;
          if (hangul_vindex >= 0) and (hangul_vindex < UTF8PROC_HANGUL_VCOUNT) then
          begin
            starter^ := UTF8PROC_HANGUL_SBASE + (hangul_lindex * UTF8PROC_HANGUL_VCOUNT + hangul_vindex) * UTF8PROC_HANGUL_TCOUNT;
            starter_property := nil;
            continue;
          end;
        end;
        hangul_sindex := starter^ - UTF8PROC_HANGUL_SBASE;
        if (hangul_sindex >= 0) and (hangul_sindex < UTF8PROC_HANGUL_SCOUNT) and ((hangul_sindex mod UTF8PROC_HANGUL_TCOUNT) = 0) then
        begin
          hangul_tindex := current_char - UTF8PROC_HANGUL_TBASE;
          if (hangul_tindex >= 0) and (hangul_tindex < UTF8PROC_HANGUL_TCOUNT) then
          begin
            starter^ := starter^ + hangul_tindex;
            starter_property := nil;
            continue;
          end;
        end;
        if starter_property = nil then
        begin
          starter_property := utf8proc_get_property(starter^);
        end;
        if (starter_property^.comb1st_index >= 0) and (current_property^.comb2nd_index >= 0) then
        begin
          composition := utf8proc_combinations[starter_property^.comb1st_index + current_property^.comb2nd_index];
          if ((composition >= 0) and
            ((0 = (options and UTF8PROC_STABLE)) or (not utf8proc_get_property(composition)^.comp_exclusion))) then
          begin
            starter^ := composition;
            starter_property := nil;
            continue;
          end;
        end;
      end;
      buffer[wpos] := current_char;
      if current_property^.combining_class <> 0 then
      begin
        if current_property^.combining_class > max_combining_class then
          max_combining_class := current_property^.combining_class;
      end
      else
      begin
        starter := buffer + wpos;
        starter_property := nil;
        max_combining_class := -1;
      end;
      inc(wpos);
    end;
    length := wpos;
  end;
  begin
    wpos := 0;
    for rpos := 0 to Pred(length) do
    begin
      uc := buffer[rpos];
      wpos := wpos + utf8proc_encode_char(uc, PByte(buffer) + wpos);
    end;
    PByte(buffer)[wpos] := (0);
    result := wpos;
  end;
end;

function utf8proc_map(str: PByte; strlen: longint; dstptr: PPByte; options: integer): longint;

var
  buffer: plongint;
  aresult: longint;
  newptr: plongint;
begin
  dstptr^ := nil;
  aresult := utf8proc_decomposer(str, strlen, nil, 0, options);
  if aresult < 0 then
  begin
    result := aresult;
    exit;
  end;
  buffer := GetMem(aresult * sizeof(longint) + 1);
  if buffer = nil then
  begin
    result := UTF8PROC_ERROR_NOMEM;
    exit;
  end;
  aresult := utf8proc_decomposer(str, strlen, buffer, aresult, options);
  if aresult < 0 then
  begin
    freemem(buffer);
    result := aresult;
    exit;
  end;
  aresult := utf8proc_reencode(buffer, aresult, options);
  if aresult < 0 then
  begin
    freemem(buffer);
    result := aresult;
    exit;
  end;
  begin
    newptr := reallocmem(buffer, aresult + 1);
    if newptr <> nil then buffer := newptr;
  end;
  dstptr^ := PByte(buffer);
  result := aresult;
end;

function utf8proc_NFD(str: PChar): PChar;
var
  retval: PByte;
begin
  utf8proc_map(PByte(str), 0, @retval, UTF8PROC_NULLTERM or UTF8PROC_STABLE or UTF8PROC_DECOMPOSE);
  result := PChar(retval);
end;

function utf8proc_NFC(str: PChar): PChar;
var
  retval: PByte;
begin
  utf8proc_map(PByte(str), 0, @retval, UTF8PROC_NULLTERM or UTF8PROC_STABLE or UTF8PROC_COMPOSE);
  result := PChar(retval);
end;

function utf8proc_NFKD(str: PChar): PChar;
var
  retval: PByte;
begin
  utf8proc_map(PByte(str), 0, @retval, UTF8PROC_NULLTERM or UTF8PROC_STABLE or UTF8PROC_DECOMPOSE or UTF8PROC_COMPAT);
  result := PChar(retval);
end;

function utf8proc_NFKC(str: PChar): PChar;
var
  retval: PByte;
begin
  utf8proc_map(PByte(str), 0, @retval, UTF8PROC_NULLTERM or UTF8PROC_STABLE or UTF8PROC_COMPOSE or UTF8PROC_COMPAT);
  result := PChar(retval);
end;

function utf8proc_getinfostring(pr: putf8proc_property_t; Chara: Longint = -1): string;
var i: integer;
begin
  Result := '';
  if Chara > -1 then
  begin
    for i := 0 to MaxUnicodeRanges do
      if (Chara >= UnicodeRanges[i].S) and (Chara <= UnicodeRanges[i].E) then
      begin
        Result := Result + 'Range: ' + UnicodeRanges[i].PG + LineEnding;
        break;
      end;
  end;
  Result := Result + 'Category: ' + CategoryStrings[pr^.category] + LineEnding;
  Result := Result + 'BIDI: ' + BIDIStrings[pr^.bidi_class] + LineEnding;
  Result := Result + 'BIDI Mirrored: ' + BoolToStr(pr^.bidi_mirrored, true) + LineEnding;
  Result := Result + 'Decomp Type: ' + DecompStrings[pr^.decomp_type];
  if pr^.lowercase_mapping > -1 then
    Result := Result + LineEnding + 'LowerCase: ' + UnicodeToUTF8(pr^.lowercase_mapping);
  if pr^.uppercase_mapping > -1 then
    Result := Result + LineEnding + 'UpperCase: ' + UnicodeToUTF8(pr^.uppercase_mapping);
  if pr^.titlecase_mapping > -1 then
    Result := Result + LineEnding + 'TitleCase: ' + UnicodeToUTF8(pr^.titlecase_mapping);
end;

end.
