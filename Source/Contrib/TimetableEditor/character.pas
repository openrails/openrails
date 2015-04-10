unit character;

{$MODE objfpc}{$H+}

interface

uses
  Classes, SysUtils, LCLProc, unicodeinfo;

const
  //Some guesswork here ;-)
  ucUpper=[UTF8PROC_CATEGORY_LU];
  ucLower=[UTF8PROC_CATEGORY_LL];
  ucTitle=[UTF8PROC_CATEGORY_LT];
  ucLetter = ucUpper+ucLower+ucTitle+[UTF8PROC_CATEGORY_LM, UTF8PROC_CATEGORY_LO];
  ucDigit = [UTF8PROC_CATEGORY_ND];
  ucNumber = ucDigit + [UTF8PROC_CATEGORY_NL, UTF8PROC_CATEGORY_NO];
  ucLetterOrDigit = ucLetter + ucDigit;
  ucControl = [UTF8PROC_CATEGORY_CC{, UTF8PROC_CATEGORY_CF, UTF8PROC_CATEGORY_CS, UTF8PROC_CATEGORY_CO, UTF8PROC_CATEGORY_CN}];
  ucWhiteSpace = [UTF8PROC_CATEGORY_ZS];
  ucPunctuation = [UTF8PROC_CATEGORY_PC, UTF8PROC_CATEGORY_PD, UTF8PROC_CATEGORY_PS, UTF8PROC_CATEGORY_PE, UTF8PROC_CATEGORY_PI, UTF8PROC_CATEGORY_PF, UTF8PROC_CATEGORY_PO];
  ucSymbol = [UTF8PROC_CATEGORY_SM, UTF8PROC_CATEGORY_SC, UTF8PROC_CATEGORY_SK, UTF8PROC_CATEGORY_SO];
  ucSeparator = [UTF8PROC_CATEGORY_ZS, UTF8PROC_CATEGORY_ZL, UTF8PROC_CATEGORY_ZP];
  ucSurrogate= [UTF8PROC_CATEGORY_CS];


type

  { TCharacter }

  TCharacter = class
  public
    class function GetUnicodeCategory(AChar: UCS4Char): Smallint; overload;
    class function IsLetterOrDigit(AChar: UCS4Char): Boolean; overload;
    class function IsLetter(AChar: UCS4Char): Boolean; overload;
    class function IsDigit(AChar: UCS4Char): Boolean; overload;
    class function IsNumber(AChar: UCS4Char): Boolean; overload;
    class function IsControl(AChar: UCS4Char): Boolean; overload;
    class function IsWhiteSpace(AChar: UCS4Char): Boolean; overload;
    class function IsPunctuation(AChar: UCS4Char): Boolean; overload;
    class function IsSymbol(AChar: UCS4Char): Boolean; overload;
    class function IsSeparator(AChar: UCS4Char): Boolean; overload;
    class function IsSurrogate(AChar: UCS4Char): Boolean; overload;
    class function IsUpper(AChar: UCS4Char): Boolean; overload;
    class function IsLower(AChar: UCS4Char): Boolean; overload;
    class function IsTitle(AChar: UCS4Char): Boolean; overload;
    class function toUpper(AChar: UCS4Char): UCS4Char; overload;
    class function toLower(AChar: UCS4Char): UCS4Char; overload;
    class function toTitle(AChar: UCS4Char): UCS4Char; overload;

    class function GetUnicodeCategory(AChar: UTF8String): Smallint; overload;
    class function IsLetterOrDigit(AChar: UTF8String): Boolean; overload;
    class function IsLetter(AChar: UTF8String): Boolean; overload;
    class function IsDigit(AChar: UTF8String): Boolean; overload;
    class function IsNumber(AChar: UTF8String): Boolean; overload;
    class function IsControl(AChar: UTF8String): Boolean; overload;
    class function IsWhiteSpace(AChar: UTF8String): Boolean; overload;
    class function IsPunctuation(AChar: UTF8String): Boolean; overload;
    class function IsSymbol(AChar: UTF8String): Boolean; overload;
    class function IsSeparator(AChar: UTF8String): Boolean; overload;
    class function IsSurrogate(AChar: UTF8String): Boolean; overload;
    class function IsUpper(AChar: UTF8String): Boolean; overload;
    class function IsLower(AChar: UTF8String): Boolean; overload;
    class function IsTitle(AChar: UTF8String): Boolean; overload;
    class function toUpper(AChar: UTF8String): UTF8String; overload;
    class function toLower(AChar: UTF8String): UTF8String; overload;
    class function toTitle(AChar: UTF8String): UTF8String; overload;

    class function Normalize_NFD(AString: UTF8String): UTF8String;
    class function Normalize_NFC(AString: UTF8String): UTF8String;
    class function Normalize_NFKD(AString: UTF8String): UTF8String;
    class function Normalize_NFKC(AString: UTF8String): UTF8String;
  end;

implementation

function UTF8CharToUCS4Char(AValue:UTF8String):UCS4Char;
var dum:integer;
begin
  Result:=UTF8CharacterToUnicode(Pchar(AValue), dum);
end;

{ TCharacter }

class function TCharacter.IsLetterOrDigit(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucLetterOrDigit;
end;

class function TCharacter.IsLetter(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucLetter;
end;

class function TCharacter.IsDigit(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucDigit;
end;

class function TCharacter.IsNumber(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucNumber;
end;

class function TCharacter.IsControl(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucControl;
end;

class function TCharacter.IsWhiteSpace(AChar: UCS4Char): Boolean;
begin
  Result :=(AChar in [$09, $0a, $0b, $0c, $0d, $85]) or
  (AChar=$2028) or (AChar=$2029) or
  (GetUnicodeCategory(AChar) in ucWhiteSpace);
end;

class function TCharacter.IsPunctuation(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucPunctuation;
end;

class function TCharacter.IsSymbol(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucSymbol;
end;

class function TCharacter.IsSeparator(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucSeparator;
end;

class function TCharacter.IsSurrogate(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucSurrogate;
end;

class function TCharacter.IsUpper(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucUpper;
end;

class function TCharacter.IsLower(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucLower;
end;

class function TCharacter.IsTitle(AChar: UCS4Char): Boolean;
begin
  Result := GetUnicodeCategory(AChar) in ucTitle;
end;

class function TCharacter.toUpper(AChar: UCS4Char): UCS4Char;
var Mapping:LongInt;
begin
  Mapping:=unicodeinfo.utf8proc_get_property(AChar)^.uppercase_mapping;
  if Mapping=-1 then Result:=AChar else Result:=Mapping;
end;

class function TCharacter.toLower(AChar: UCS4Char): UCS4Char;
var Mapping:LongInt;
begin
  Mapping:=unicodeinfo.utf8proc_get_property(AChar)^.lowercase_mapping;
  if Mapping=-1 then Result:=AChar else Result:=Mapping;
end;

class function TCharacter.toTitle(AChar: UCS4Char): UCS4Char;
var Mapping:LongInt;
begin
  Mapping:=unicodeinfo.utf8proc_get_property(AChar)^.titlecase_mapping;
  if Mapping=-1 then Result:=AChar else Result:=Mapping;
end;

class function TCharacter.GetUnicodeCategory(AChar: UTF8String): Smallint;
begin
  Result:=GetUnicodeCategory(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsLetterOrDigit(AChar: UTF8String): Boolean;
begin
  Result:=IsLetterOrDigit(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsLetter(AChar: UTF8String): Boolean;
begin
  Result:=IsLetter(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsDigit(AChar: UTF8String): Boolean;
begin
  Result:=IsDigit(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsNumber(AChar: UTF8String): Boolean;
begin
  Result:=IsNumber(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsControl(AChar: UTF8String): Boolean;
begin
  Result:=IsControl(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsWhiteSpace(AChar: UTF8String): Boolean;
begin
  Result:=IsWhiteSpace(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsPunctuation(AChar: UTF8String): Boolean;
begin
  Result:=IsPunctuation(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsSymbol(AChar: UTF8String): Boolean;
begin
  Result:=IsSymbol(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsSeparator(AChar: UTF8String): Boolean;
begin
  Result:=IsSeparator(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsSurrogate(AChar: UTF8String): Boolean;
begin
  Result:=IsSurrogate(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsUpper(AChar: UTF8String): Boolean;
begin
  Result:=IsUpper(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsLower(AChar: UTF8String): Boolean;
begin
  Result:=IsLower(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.IsTitle(AChar: UTF8String): Boolean;
begin
  Result:=IsTitle(UTF8CharToUCS4Char(AChar));
end;

class function TCharacter.toUpper(AChar: UTF8String): UTF8String;
begin
  Result:=UnicodeToUTF8(toUpper(UTF8CharToUCS4Char(AChar)));
end;

class function TCharacter.toLower(AChar: UTF8String): UTF8String;
begin
  Result:=UnicodeToUTF8(toLower(UTF8CharToUCS4Char(AChar)));
end;

class function TCharacter.toTitle(AChar: UTF8String): UTF8String;
begin
  Result:=UnicodeToUTF8(toTitle(UTF8CharToUCS4Char(AChar)));
end;

class function TCharacter.Normalize_NFD(AString: UTF8String): UTF8String;
  var pc:PChar;
begin
    pc:=utf8proc_NFD(PChar(AString));
    Result:=pc;
    freemem(pc);
end;

class function TCharacter.Normalize_NFC(AString: UTF8String): UTF8String;
var pc:PChar;
begin
  pc:=utf8proc_NFC(PChar(AString));
  Result:=pc;
  freemem(pc);
end;

class function TCharacter.Normalize_NFKD(AString: UTF8String): UTF8String;
var pc:PChar;
begin
  pc:=utf8proc_NFKD(PChar(AString));
  Result:=pc;
  freemem(pc);
end;

class function TCharacter.Normalize_NFKC(AString: UTF8String): UTF8String;
var pc:PChar;
begin
  pc:=utf8proc_NFKC(PChar(AString));
  Result:=pc;
  freemem(pc);
end;

class function TCharacter.GetUnicodeCategory(AChar: UCS4Char): Smallint;
begin
  Result := unicodeinfo.utf8proc_get_property(AChar)^.category;
end;

end.
