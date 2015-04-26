unit trains;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, StdCtrls,
  ComCtrls, Buttons, maskedit, timetabledata;

type

  { TForm3 }

  TForm3 = class(TForm)
    BitBtn1: TBitBtn;
    BitBtn2: TBitBtn;
    CheckBox2: TCheckBox;
    CheckBox3: TCheckBox;
    CheckBox4: TCheckBox;
    CheckBox5: TCheckBox;
    CheckBox6: TCheckBox;
    CheckBox7: TCheckBox;
    CheckBox8: TCheckBox;
    CheckBox9: TCheckBox;
    Edit1: TEdit;
    Edit2: TEdit;
    Label1: TLabel;
    Label2: TLabel;
    Label3: TLabel;
    ListBox3: TListBox;
    ListBox4: TListBox;
    ListView1: TListView;
    ListView2: TListView;
    MaskEdit1: TMaskEdit;
    MaskEdit2: TMaskEdit;
    SpeedButton1: TSpeedButton;
    SpeedButton2: TSpeedButton;
    SpeedButton3: TSpeedButton;
    SpeedButton4: TSpeedButton;
    SpeedButton5: TSpeedButton;
    SpeedButton6: TSpeedButton;
    SpeedButton7: TSpeedButton;
    SpeedButton8: TSpeedButton;
    StaticText1: TStaticText;
    StaticText2: TStaticText;
    StaticText3: TStaticText;
    StaticText4: TStaticText;
    StaticText5: TStaticText;
    StaticText6: TStaticText;
    StaticText7: TStaticText;
    procedure BitBtn1Click(Sender: TObject);
    procedure BitBtn2Click(Sender: TObject);
    procedure CheckBox2Change(Sender: TObject);
    procedure CheckBox3Change(Sender: TObject);
    procedure CheckBox4Change(Sender: TObject);
    procedure CheckBox5Change(Sender: TObject);
    procedure CheckBox6Change(Sender: TObject);
    procedure CheckBox7Change(Sender: TObject);
    procedure CheckBox8Change(Sender: TObject);
    procedure CheckBox9Change(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure FormShow(Sender: TObject);
    procedure ListBox1DblClick(Sender: TObject);
    procedure ListBox2DblClick(Sender: TObject);
    procedure ListBox4Click(Sender: TObject);
    procedure ListView1DblClick(Sender: TObject);
    procedure ListView2DblClick(Sender: TObject);
    procedure SpeedButton1Click(Sender: TObject);
    procedure SpeedButton2Click(Sender: TObject);
    procedure SpeedButton3Click(Sender: TObject);
    procedure SpeedButton4Click(Sender: TObject);
    procedure SpeedButton5Click(Sender: TObject);
    procedure SpeedButton6Click(Sender: TObject);
    procedure SpeedButton7Click(Sender: TObject);
    procedure SpeedButton8Click(Sender: TObject);
    procedure updateConsistLabel();
    procedure showConsists;
    function pruefzeit(zeit: string): string;
    procedure fillpathview;
    procedure fillconsistsview;
    procedure loadtrains();
  private
    { private declarations }
  public
    { public declarations }
  end;

var
  Form3: TForm3;
  col: integer;

  resourcestring
    irgendwas = 'irgndwas';
    add = 'add';
    choose = 'choose';

implementation
uses unit1;

{$R *.lfm}


{ TForm3 }

procedure TForm3.updateConsistLabel();
var i: integer;
begin
  label2.caption:='';
  for i:= 0 to listbox3.items.count -1 do begin
    label2.caption:=label2.caption+'+'+listbox3.items[i];
  end;
  label2.caption:=copy(label2.caption,2,length(label2.caption));
end;

procedure TForm3.showConsists;
var cell: String;
    i: integer;
    ins: boolean;
    str: String;
begin
  listbox3.clear;
  cell:=form1.grid.cells[col,getRow('#consist')];
  if cell <> '' then begin
      ins:=false;
      for i:= 1 to length(cell)-1 do begin
        if cell[i] = '<' then ins:=true;
        if ( cell[i] = '+' ) and ( ins = false )then begin
          listbox3.items.add(trim(copy(cell,1,i-1)));
          delete(cell,1,i);
        end;
        if cell[i] = '>' then begin
          ins:=false;
          listbox3.items.add(trim(copy(cell,1,i)));
          delete(cell,1,i+1);
        end;
      end;
      listbox3.items.add(trim(cell));
      label2.caption:='';
    for i:= 0 to listbox3.items.count -1 do begin
      label2.caption:=label2.caption+' + '+listbox3.items[i];
    end;
    label2.caption:=copy(label2.caption,4,length(label2.caption));
    if listbox3.items.count > 1 then checkbox2.Checked:=true;
  end else begin
    label2.caption:='';
    checkbox2.checked:=false;
  end;
end;

procedure TForm3.SpeedButton1Click(Sender: TObject);
var li: tlistitem;
begin
  if listview1.ItemIndex >-1 then begin
    li:=listview1.Items[listview1.itemindex];
    label1.caption:=li.Caption;
  end;
end;

procedure TForm3.SpeedButton2Click(Sender: TObject);
var i: integer;
    li: tlistitem;
begin
  if checkbox2.checked then begin  // multi consist Yes
    if listview2.itemindex > -1 then begin  // List not empty
      label2.caption:='';                   // Delete Label
      li:=listview2.Items[listview2.itemindex];       // take item
      if ( pos('$',li.Caption) > 0 ) or ( pos ('+',li.caption) > 0 ) then // contains $ or +
        listbox3.items.add('<'+li.caption+'>')
      else listbox3.items.add(li.caption);
    end;
    if listbox3.items.count > 1 then begin
      for i:=0 to listbox3.items.count -1 do begin
        label2.caption:=label2.caption+' + '+listbox3.items[i];
      end;
      label2.caption:=copy(label2.caption,4,length(label2.caption));
    end else label2.caption:=listbox3.items[0];
  end else begin  // multi consist No
    if listview2.itemindex > -1 then begin
      li:=listview2.items[listview2.itemindex];
      if ( pos('$',li.Caption) > 0 ) or ( pos('+',li.caption) > 0 ) then
        label2.caption:='<'+li.caption+'>'
      else label2.caption:=li.caption;
    end;
  end;
{  if checkbox2.Checked then begin
    label2.caption:='';
    if ( pos('$',listbox2.items[listbox2.itemindex]) >0 ) or ( pos('+',listbox2.Items[listbox2.itemindex]) >0 ) then
      listbox3.items.add('<'+listbox2.items[listbox2.itemindex]+'>')
      else listbox3.items.add(listbox2.Items[listbox2.ItemIndex]);
    if listbox3.items.count > 1 then begin
      for i:=0 to listbox3.items.Count -1 do begin
        label2.caption:=label2.caption+'+'+listbox3.items[i];
      end;
      label2.caption:=copy(label2.caption,2,length(label2.caption));
    end else label2.caption:=listbox3.items[0];
  end else begin
      if ( pos('$',listbox2.items[listbox2.itemindex]) >0 ) or ( pos('+',listbox2.Items[listbox2.itemindex]) >0 ) then
        label2.caption:='<'+listbox2.items[listbox2.itemindex]+'>'
      else label2.caption:=listbox2.Items[listbox2.ItemIndex];
  end;}
end;

procedure TForm3.SpeedButton3Click(Sender: TObject);
begin
  loadpaths(getpathspath);
//  listbox1.items:=getpaths;
  listview1.Clear;
  fillpathview;
end;

procedure TForm3.SpeedButton4Click(Sender: TObject);
begin
  loadconsists(getconsistspath);
  listview2.Clear;
  fillconsistsview;
end;

procedure TForm3.SpeedButton5Click(Sender: TObject);
var oldindex: integer;
    item: String;
begin
  if listbox3.itemindex > 0 then begin
    oldindex:=listbox3.itemindex;
    item:=listbox3.items[oldindex];
    listbox3.items.delete(oldindex);
    listbox3.items.Insert(oldindex -1,item);
    listbox3.itemindex:=oldindex -1;
    updateconsistlabel;
  end;
end;

procedure TForm3.SpeedButton6Click(Sender: TObject);
var oldindex: integer;
begin
  if ( listbox3.itemindex < listbox3.Count -1 ) and ( listbox3.itemindex > -1) then begin
    oldindex:=listbox3.itemindex;
    listbox3.items.Insert(listbox3.itemindex +2,listbox3.Items[listbox3.itemindex]);
    listbox3.items.delete(listbox3.itemindex);
    listbox3.itemindex:=oldindex +1;
    updateconsistlabel;
  end;
end;

procedure TForm3.SpeedButton7Click(Sender: TObject);
var oldindex: integer;
begin
  if ( listbox3.itemindex > -1 ) then begin
    oldindex:=listbox3.itemindex;
    listbox3.Items.Delete(listbox3.itemindex);
    if listbox3.items.count > 0 then begin
      if oldindex < listbox3.items.count -1 then listbox3.itemindex:=oldindex else listbox3.itemindex:=listbox3.items.count -1;
      updateconsistlabel;
    end else label2.caption:='';
  end;
end;

procedure TForm3.SpeedButton8Click(Sender: TObject);
var i: integer;
begin
  if checkbox2.checked then begin
    if listbox3.itemindex >-1 then begin
      if pos('$reverse', listbox3.Items[listbox3.itemindex]) > 0 then listbox3.items[listbox3.itemindex]:=trimright(leftstr(listbox3.items[listbox3.itemindex],pos('$reverse',listbox3.items[listbox3.itemindex])-1))
      else listbox3.items[listbox3.itemindex]:=listbox3.items[listbox3.itemindex]+' $reverse';
      label2.caption:='';
      for i:= 0 to listbox3.items.count -1 do begin
        label2.caption:=label2.caption+' + '+listbox3.items[i];
      end;
      label2.caption:=copy(label2.caption,2,length(label2.caption));
    end;
  end else begin
    if pos('$reverse',Label2.caption) > 0 then Label2.caption:=trimright(leftstr(label2.caption,pos('$reverse',label2.caption)-1))
    else label2.caption:=label2.caption+' $reverse';
  end;
end;

procedure TForm3.FormCreate(Sender: TObject);
begin

end;

procedure TForm3.BitBtn1Click(Sender: TObject);
var start: string;
begin
  form1.grid.cells[col,0]:=edit1.text;
  form1.grid.cells[col,1]:=edit2.text;
  setRow('#path',label1.caption);
  setRow('#consist',label2.caption);
  start:=maskedit2.text;
  if checkbox3.checked then start:=start+' $create='+maskedit1.text;
  if label3.caption <> '' then start:=start + ' /ahead='+label3.caption;
  setRow('#start',start);
  form1.shadowupdate;
end;

procedure TForm3.BitBtn2Click(Sender: TObject);
begin
  form3.close;
end;

procedure TForm3.CheckBox2Change(Sender: TObject);
begin
  if checkbox2.checked then begin
    speedbutton2.caption:=add;
//    listbox3.clear;
    listbox3.Visible:=true;
    speedbutton5.visible:=true;
    speedbutton6.visible:=true;
    speedbutton7.visible:=true;
  end
  else begin
     speedbutton2.caption:=choose;
     listbox3.clear;
     listbox3.visible:=false;
     speedbutton5.visible:=false;
     speedbutton6.visible:=false;
     speedbutton7.visible:=false;
  end;
end;

procedure TForm3.CheckBox3Change(Sender: TObject);
begin
  maskedit1.Enabled:=checkbox3.Checked;
end;

procedure TForm3.CheckBox4Change(Sender: TObject);
var i: integer;
begin
  listbox4.Enabled:=checkbox4.Checked;
  if checkbox4.Checked = false then begin
    label3.caption:='';
    checkbox9.enabled:=false;
  end;
  if checkbox4.checked then begin
    checkbox9.enabled:=true;
    loadtrains;
  end;
end;

procedure tform3.loadtrains();
begin
 listbox4.clear;
 listbox4.items:=form1.gettrains(checkbox9.checked);
end;

procedure TForm3.CheckBox5Change(Sender: TObject);
begin
  fillconsistsview;
end;

procedure TForm3.CheckBox6Change(Sender: TObject);
begin
  fillconsistsview;
end;

procedure TForm3.CheckBox7Change(Sender: TObject);
begin
  fillpathview;
end;

procedure TForm3.CheckBox8Change(Sender: TObject);
begin
  fillpathview
end;

procedure TForm3.CheckBox9Change(Sender: TObject);
begin
  loadtrains();
end;

procedure TForm3.FormShow(Sender: TObject);
var sta,i,p: integer;
    str: string;
begin
  col:=form1.grid.col;
  setcol(col);
  edit1.Text:=form1.grid.cells[col,0];
  edit2.text:=form1.grid.cells[col,1];
  label1.caption:=form1.grid.cells[col,getRow('#path')];
  //label2.caption:=form1.grid.cells[col,getRow('#consist')];
  showconsists;
  checkbox3.checked:=false;
  maskedit2.text:='';
  maskedit1.text:='';
  checkbox4.checked:=false;
  label3.caption:='';
  str:=form1.grid.cells[col,getRow('#start')];
  if str <> '' then begin
    if pos ('$create=',str) > 0 then begin
      checkbox3.checked:=true;
      p:=pos('$create=',str);
      for i:=p to length(str) do begin
          if ( str[i] = ' ' ) or ( str[i] = '/' ) or ( i = length(str) ) then begin
            maskedit1.Text:=trim(copy(str,p+length('$create='),i-p-length('$create=')+1));
          break;
          end;
      end;
    end;
    if pos ('/ahead=', str) > 0 then begin
      checkbox4.checked:=true;
      p:=pos('/ahead=',str);
      for i:=p+1 to length(str) do begin
          if ( str[i] = '/' ) then begin
            label3.caption:=trim(copy(str,p+length('/ahead='), i-p-length('/ahead=')));
            break;
          end;
          if ( i = length(str) ) then begin
            label3.caption:=trim(copy(str,p+length('/ahead='),length(str)));
          end;
      end;
    end;
    for i:= 0 to length(str) do begin
      if ( str[i] = ' ' ) or ( str[i] = '$' ) or ( str[i] = '/' ) or (i=length(str)) then begin
        maskedit2.text:=trim(copy(str,0,i));
        break;
      end;
    end;
  end;
{  maskedit2.text:=form1.grid.cells[col,getRow('#start')];
  if pos('$create=',maskedit2.text) > 0 then begin
     checkbox3.Checked:=true;
     p:=pos('$create=',maskedit2.text);
     for i:=p to length(maskedit2.text) do begin
       if ( maskedit2.text[i] = ' ' ) or ( i = length(maskedit2.text) ) then begin
         maskedit1.text:=trim(copy(maskedit2.text,p+length('$create='),i-p-length('$create=')));
         str:=maskedit2.text;
         delete(str,p,i-p);
         maskedit2.text:=trim(str);
         break;
       end;
     end;
  end;
  if pos('/ahead=',maskedit2.text) > 0 then begin
    checkbox4.checked:=true;
    p:=pos('/ahead=',maskedit2.text);
    for i:=p to length(maskedit2.text) do begin
      if (maskedit2.text[i] = '/' ) or ( i = length(maskedit2.text) ) then begin
        label3.Caption:=trim(copy(maskedit2.text,p+length('/ahead='),i));
        str:=maskedit2.text;
        delete(str,p,length(str));
        maskedit2.text:=trim(str);
      end;
    end;
  end;}
  fillpathview;
  fillconsistsview;
end;

procedure tform3.fillpathview;
var i: integer;
    paths: tstringlist;
    pathnames: tstringlist;
    li: tListItem;
    pa: string;
begin
  listview1.clear;
  paths:=tstringlist.create;
  pathnames:=tstringlist.create;
  paths:=getpaths;
  pathnames:=getpathnames;
  for i:=0 to paths.count -1 do begin
    pa:=paths[i];
    if (( checkbox7.Checked) and ( leftstr(paths[i],4) <> 'sid_' ) ) or
       (( checkbox8.checked) and ( leftstr(paths[i],4) = 'sid_' ) ) then begin
      li:=listview1.Items.add;
      li.Caption:=paths[i];
      li.SubItems.Add(pathnames[i]);
    end;
  end;
end;

procedure tform3.fillconsistsview;
var i: integer;
    consists: tstringlist;
    consistnames: tstringlist;
    consisttypes: tstringlist;
    li: tlistitem;
begin
  listview2.Clear;
  consists:=tstringlist.create;
  consistnames:=tstringlist.create;
  consisttypes:=tstringlist.create;
  consists:=getconsists;
  consistnames:=getconsistsnames;
  consisttypes:=getconsisttypes;
  for i:=0 to consists.count -1 do begin
    if (( checkbox5.Checked ) and ( consisttypes[i] = 'e' ) ) or
       (( checkbox6.checked ) and ( consisttypes[i] = 'w' ) ) then begin
      li:=listview2.items.add;
      li.caption:=consists[i];
      li.subitems.add(consistnames[i]);
    end;
  end;
end;

function tform3.pruefzeit(zeit: string): string;
var zt: string;
begin
  zt:=zeit;
  if length(zeit) <> 8 then begin
    if length(zeit) > 8 then zt:='';
  end;
end;

procedure TForm3.ListBox1DblClick(Sender: TObject);
begin
  speedbutton1.Click;
end;




procedure TForm3.ListBox2DblClick(Sender: TObject);
begin
  speedbutton2.Click;
end;

procedure TForm3.ListBox4Click(Sender: TObject);
begin
  if listbox4.items.count > 0 then label3.caption:=listbox4.Items[listbox4.itemindex];
end;

procedure TForm3.ListView1DblClick(Sender: TObject);
begin
  speedbutton1.click;
end;

procedure TForm3.ListView2DblClick(Sender: TObject);
begin
  speedbutton2.click;
end;



end.

