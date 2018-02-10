unit utf8scanner;

{$MODE objfpc}{$H+}

interface

uses
  Classes, SysUtils, LazUTF8;      


type

{ TUTF8Scanner }

  TUTF8Scanner = class
  private
    fBytePos: integer;
    fCharPos: integer;
    fCharLength: integer;
    fLen: integer;
    fAnsiString: AnsiString;
    fDone: Boolean;
    fFindList: TList;
    function GetString: AnsiString;
    function GetUCS4Char(Index: Integer): UCS4Char;
    function GetUTF8Char(Index: Integer): AnsiString;
    procedure PutUCS4Char(Index: Integer; const AValue: UCS4Char);
    procedure PutUTF8Char(Index: Integer; const AValue: AnsiString);
    procedure SetFindChars(Chars: AnsiString);
    procedure SetString(const AValue: AnsiString);
  public
    constructor Create(AString: AnsiString); overload;
    destructor Destroy; override;
    function Next: UCS4Char;
    property Done: Boolean read fDone;
    procedure Reset;
    function Length: Cardinal;
    function FindIndex(AChar: UCS4Char): Integer;
    function IsFindChar(AChar: UCS4Char): Boolean;
    procedure Replace(AChar: AnsiString; ScanMode:Boolean=true);
    function CurrentCharAsUTF8: AnsiString;
    function CurrentCharAsUCS4: UCS4Char;
    function CharToUnicode(AChar: AnsiString): UCS4Char;
    function GenerateCaseStatement(WithBeginEnd: Boolean = false; InstanceName: string = 'US'): string;
    procedure SeekPos(CharPos: Cardinal);
    procedure DebugInfo;
    property FindChars: AnsiString write SetFindChars;
    property BytePos: integer read fBytePos;
    property CharPos: integer read fCharPos;
    property CharLength: integer read fCharLength;
    property UTF8String: AnsiString read GetString write SetString;
    property UTF8Chars[Index: Integer]: AnsiString read GetUTF8Char write PutUTF8Char; default;
    property UCS4Chars[Index: Integer]: UCS4Char read GetUCS4Char write PutUCS4Char;
  end;

const UTF8Type: array[0..255] of Shortint =
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


implementation

{ TUTF8Scanner }


function TUTF8Scanner.GetString: AnsiString;
begin
  Result := fAnsiString;
end;

function TUTF8Scanner.GetUCS4Char(Index: Integer): UCS4Char;
begin
  if fLen = 0 then begin Reset; exit; end;
  SeekPos(Index);
  Result := CurrentCharAsUCS4;
end;

function TUTF8Scanner.GetUTF8Char(Index: Integer): AnsiString;
begin
  if fLen = 0 then begin Reset; exit; end;
  SeekPos(Index);
  Result := CurrentCharAsUTF8;
end;

procedure TUTF8Scanner.PutUCS4Char(Index: Integer; const AValue: UCS4Char);
begin
  if fLen = 0 then begin Reset; exit; end;
  SeekPos(Index);
  Replace(UnicodeToUTF8(AValue),false);
end;

procedure TUTF8Scanner.PutUTF8Char(Index: Integer; const AValue: AnsiString);
begin
  if fLen = 0 then begin Reset; exit; end;
  SeekPos(Index);
  Replace(AValue,false);
end;

constructor TUTF8Scanner.Create(AString: AnsiString);
begin
  fFindList := TList.Create;
  SetString(AString);
end;

procedure TUTF8Scanner.SetString(const AValue: AnsiString);
begin
  fAnsiString := AValue;
  fLen := System.Length(AValue);
  Reset;
end;

procedure TUTF8Scanner.Reset;
begin
  fBytePos := 1;
  if fLen > 0 then fCharLength := UTF8Type[Ord(fAnsiString[fBytepos])] else fCharLength := 0;
  fCharPos := 1;
  fDone := False;
end;

procedure TUTF8Scanner.DebugInfo;
begin
  Writeln('BytePos: ', fBytePos, ' CharPos: ', fCharPos, ' LastLen: ', fCharLength, ' CC: ', CurrentCharAsUTF8);
  Writeln('..............');
end;


destructor TUTF8Scanner.Destroy;
begin
  fFindList.free;
  inherited Destroy;
end;

function TUTF8Scanner.Next: UCS4Char;
var Len: integer;
begin
  if fBytePos <= fLen then
  begin
    Result := UTF8CharacterToUnicode(@fAnsiString[fBytePos], Len);
    fCharLength := Len;
    inc(fBytePos, Len);
    inc(fCharPos);
    if fBytePos > fLen then fDone := true;
  end else fDone := true;
end;


procedure TUTF8Scanner.SeekPos(CharPos: Cardinal);
var res: Byte;
begin
  if fLen = 0 then begin Reset; exit; end;
  if (CharPos < 1) then CharPos := 1 else if (CharPos > fLen) then CharPos := fLen;
  if fCharPos > CharPos then if (fCharPos - CharPos) > (fCharPos div 3) then Reset; //Fwd seeking is faster
  if fCharPos < CharPos then
  begin
    while (fCharPos <> CharPos) and (fBytepos <= fLen) do
    begin
      res := UTF8Type[Ord(fAnsiString[fBytepos])];
      if res > 0 then
      begin
        inc(fCharPos);
        inc(fBytepos, res);
      end else inc(fBytePos);
    end;
    fCharLength := UTF8Type[Ord(fAnsiString[fBytepos])];
  end else
    if fCharPos > CharPos then
    begin
      dec(fBytePos);
      while (fCharPos <> CharPos) and (fBytepos > 0) do
      begin
        res := UTF8Type[Ord(fAnsiString[fBytepos])];
        if res > 0 then
        begin
          dec(fCharPos);
          if fCharPos <> CharPos then dec(fBytepos);
        end else dec(fBytePos);
      end;
      fCharLength := UTF8Type[Ord(fAnsiString[fBytepos])];
    end;
end;


procedure TUTF8Scanner.SetFindChars(Chars: AnsiString);
var Scn: TUTF8Scanner;
begin
  Scn := TUTF8Scanner.Create(Chars);
  fFindList.Clear;
  if System.Length(Chars) > 0 then
    repeat
      fFindList.Add(Pointer(Scn.Next));
    until Scn.Done;
  Scn.free;
end;

function TUTF8Scanner.IsFindChar(AChar: UCS4Char): Boolean;
begin
  Result := fFindList.IndexOf(Pointer(AChar)) > -1;
end;

function TUTF8Scanner.FindIndex(AChar: UCS4Char): Integer;
begin
  Result := fFindList.IndexOf(Pointer(AChar));
end;


procedure TUTF8Scanner.Replace(AChar: AnsiString; ScanMode:Boolean=true);
var i, diff: integer;
begin
  if fLen = 0 then begin Reset; exit; end;
  if ScanMode then SeekPos(fCharPos-1);
  if System.Length(AChar) = fCharLength then
  begin
    for i := 0 to fCharLength - 1 do fAnsiString[fBytePos + i] := Achar[i + 1];
  end else
  begin
    System.Delete(fAnsiString, fBytePos, fCharLength);
    System.Insert(AChar, fAnsiString, fBytePos);
    diff := System.Length(AChar) - fCharLength;
    Inc(fLen, diff);
    fCharLength := System.Length(AChar);
  end;
   if ScanMode then Next;
end;

function TUTF8Scanner.CurrentCharAsUTF8: AnsiString;
begin
  Result := Copy(fAnsiString, fBytePos, fCharLength);
end;

function TUTF8Scanner.CurrentCharAsUCS4: UCS4Char;
var Dummy: integer;
begin
  Result := UTF8CharacterToUnicode(@fAnsiString[fBytePos], Dummy);
end;

function TUTF8Scanner.CharToUnicode(AChar: AnsiString): UCS4Char;
var Dummy: integer;
begin
  Result := UTF8CharacterToUnicode(@AChar[1], Dummy);
end;

function TUTF8Scanner.Length: Cardinal;
var i, res: cardinal;
begin
  if fLen = 0 then begin Result := 0; exit; end;
  i := 1;
  Result := 0;
  while i <= fLen do
  begin
    res := UTF8Type[Ord(fAnsiString[i])];
    if res > 0 then
    begin
      inc(Result);
      inc(i, res);
    end else inc(i);
  end;
end;

function TUTF8Scanner.GenerateCaseStatement(WithBeginEnd: Boolean; InstanceName: string): string;
var i: integer;
begin
  Result := 'case ' + InstanceName + '.FindIndex(' + InstanceName + '.Next) of' + LineEnding;
  for i := 0 to fFindList.Count - 1 do
    if WithBeginEnd then
    begin
      Result := Result + '  {' + UnicodeToUTF8(UCS4Char(fFindList[i])) + '} ' + inttostr(i) + ': begin <code here> end;' + LineEnding;
    end else
      Result := Result + '  {' + UnicodeToUTF8(UCS4Char(fFindList[i])) + '} ' + inttostr(i) + ': <code here> ;' + LineEnding;
  Result := Result + 'end;';
end;


end.
