unit tteobjects;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, contnrs;

type

TItemRData = class
  private
    Fval1: String;
    Fval2: String;
    Fval3: String;
    Fval4: String;
    Fval5: String;
    procedure setValues(vals: String);
  public
    property value1: String read Fval1;
    property value2: String read Fval2;
    property value3: String read Fval3;
    property value4: String read Fval4;
    property value5: String read Fval5;
end;

//TSidingData = class
//  private
//    FAnfEnde: String;
//    FPartner: String;
//    procedure setValues(vals: String);
//  public
//    property AnfEnde: String read FAnfEnde;
//    property Partner: String read FPartner;
//    function getPartner():String;
//end;

TSiding = class
  private
    FName: String;
    FItemId: String;
    FItemRData1: TItemRData;
    FItemRData2: TItemRData;
    FPartner: String;
    FGotItemRData: Boolean;
    FGotSidingData: Boolean;
    FGotItemRData2: Boolean;
    procedure setName(Name: String);
    procedure setItemId(Id: String);
  public
    property Name: String read FName write setName;
    property ItemId: String read FItemId write setItemId;
    property Partner: String read FPartner;
    procedure setItemRData(data: String; nr: integer);
    procedure setSidingData(data: String);
    function getItemId(): String;
    function getRDataValue11(): String;
    function getRDataValue12(): String;
    function getRDataValue13(): String;
    function getRDataValue14(): String;
    function getRDataValue15(): String;
    function getRDataValue21(): String;
    function getRDataValue22(): String;
    function getRDataValue23(): String;
    function getRDataValue24(): String;
    function getRDataValue25(): String;
    function getRDataValues_1(): String;
    function getRDataValues_2(): String;
    function getRDataP1():String;
    function getRDataP2():String;
    function hasItemRData(): boolean;
    function hasSidingData(): boolean;
    function hasItemRData2(): boolean;
end;

TSidingObjectList = class(TObjectList)
  protected
    function getItem(Index: Integer): TSiding; virtual;
    procedure setItem(Index: Integer; Objekt: TSiding); virtual;
  public
    function Add(Objekt: TSiding): Integer; virtual;
    function Remove(Objekt: TSiding): Integer; virtual;
    function IndexOf(Objekt: TSiding): Integer; virtual;
    procedure Insert(Index: Integer; Objekt: TSiding); virtual;
    function First: TSiding; virtual;
    function Last: TSiding; virtual;
    property Items[Index: Integer]: TSiding read getItem write setItem; default;
  end;

implementation

uses unit1;

// Hilfsfunktionen
procedure explode (const Delimiter: Char; Input: string; const Strings: TStringlist);
begin
   Assert(Assigned(Strings)) ;
   Strings.Clear;
   Strings.StrictDelimiter := true;
   Strings.Delimiter := Delimiter;
   Strings.DelimitedText := Input;
end;

// Objektfunktionen

procedure TItemRData.setValues(vals: String);
var values: tstringlist;
begin
  values:= tstringlist.create;
  explode(' ',vals, values);
  Fval1:=values[0];
  Fval2:=values[1];
  Fval3:=values[2];
  Fval4:=values[3];
  Fval5:=values[4];
end;

//procedure TSidingData.setValues(vals: String);
//var values: tstringlist;
//begin
//  values:=tstringlist.create;
//  explode(' ',vals, values);
//  FAnfEnde:=values[0];
//  FPartner:=values[1];
//  form1.Memo1.lines.add(fpartner);
//end;

//function TSidingData.getPartner():String;
//begin
//  result:=Fpartner;
//end;

procedure TSiding.setName(Name: String);
begin
  FName:=Name;
end;

procedure TSiding.setItemId(Id: String);
begin
  FItemId:=Id;
end;

function tsiding.getItemId(): String;
begin
  result:=FItemId;
end;

procedure TSiding.setItemRData(data: String; nr: integer);
begin
  if nr = 1 then begin
    FGotItemRData:=true;
    FItemRData1:=TItemRData.Create;
    FItemRData1.setValues(data);
  end;
  if nr = 2 then begin
    FGotItemRData2:=true;
    FItemRData2:=TItemRData.create;
    FItemRData2.setValues(data);
  end;
end;

procedure TSiding.setSidingData(data: String);
var values: tstringlist;
begin
  FGotSidingData:=true;
  values:=tstringlist.create;
  explode(' ',data, values);
  FPartner:=values[1];
end;

function TSiding.getRDataValue11(): String;
begin
  result:=FItemRData1.value1;
end;

function TSiding.getRDataValue12(): String;
begin
    result:=FItemRData1.value2;
end;

function TSiding.getRDataValue13(): String;
begin
    result:=FItemRData1.value3;
end;

function TSiding.getRDataValue14(): String;
begin
    result:=FItemRData1.value4;
end;

function TSiding.getRDataValue15(): String;
begin
    result:=FItemRData1.value5;
end;

function TSiding.getRDataValue21(): String;
begin
  result:=FItemRData2.value1;
end;

function TSiding.getRDataValue22(): String;
begin
    result:=FItemRData2.value2;
end;

function TSiding.getRDataValue23(): String;
begin
    result:=FItemRData2.value3;
end;

function TSiding.getRDataValue24(): String;
begin
    result:=FItemRData2.value4;
end;

function TSiding.getRDataValue25(): String;
begin
    result:=FItemRData2.value5;
end;

function TSiding.getRDataValues_1(): String;
var tmp: String;
begin
  tmp:=FItemRData1.value1+' ';
  tmp:=tmp+FItemRData1.value2+' ';
  tmp:=tmp+FItemRData1.value3+' ';
  tmp:=tmp+FItemRData1.value4+' ';
  tmp:=tmp+FItemRData1.value5;
  result:=tmp;
end;

function TSiding.getRDataValues_2(): String;
var tmp: String;
begin
  tmp:=FItemRData2.value1+' ';
  tmp:=tmp+FItemRData2.value2+' ';
  tmp:=tmp+FItemRData2.value3+' ';
  tmp:=tmp+FItemRData2.value4+' ';
  tmp:=tmp+FItemRData2.value5;
  result:=tmp;
end;

function TSiding.getRDataP1():String;
var tmp: String;
begin
  tmp:=FItemRData1.value4+' ';
  tmp:=tmp+FItemRData1.value5+' ';
  tmp:=tmp+FItemRData1.value1+' ';
  tmp:=tmp+FItemRData1.value2+' ';
  tmp:=tmp+FItemRData1.value3+' 1 0';
  result:=tmp;
end;

function TSiding.getRDataP2():String;
var tmp: String;
begin
  tmp:=FItemRData2.value4+' ';
  tmp:=tmp+FItemRData2.value5+' ';
  tmp:=tmp+FItemRData2.value1+' ';
  tmp:=tmp+FItemRData2.value2+' ';
  tmp:=tmp+FItemRData2.value3+' 1 0';
  result:=tmp;
end;

function TSiding.hasItemRData():Boolean;
begin
  result:=FGotItemRData;
end;

function TSiding.hasItemRData2():Boolean;
begin
  result:=FGotItemRData;
end;

function TSiding.hasSidingData():Boolean;
begin
  result:=FGotSidingData;
end;

function TSidingObjectList.getItem(Index: Integer): TSiding;
begin
  Result := TSiding(inherited Items[Index]);
end;

procedure TSidingObjectList.setItem(Index: Integer; Objekt: TSiding);
begin
  inherited Items[Index] := Objekt;
end;

function TSidingObjectList.Add(Objekt: TSiding): Integer;
begin
  Result := inherited Add(Objekt);
end;

function TSidingObjectList.First: TSiding;
begin
  Result := TSiding(inherited First());
end;

function TSidingObjectList.IndexOf(Objekt: TSiding): Integer;
begin
  Result := inherited IndexOf(Objekt);
end;

procedure TSidingObjectList.Insert(Index: Integer; Objekt: TSiding);
begin
  inherited Insert(Index, Objekt);
end;

function TSidingObjectList.Last: TSiding;
begin
  Result := TSiding(inherited Last());
end;

function TSidingObjectList.Remove(Objekt: TSiding): Integer;
begin
  Result := inherited Remove(Objekt);
end;

end.

