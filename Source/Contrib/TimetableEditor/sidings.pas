unit sidings;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, ComCtrls,
  StdCtrls, ExtCtrls, Buttons, LazUTF8, timetabledata, charencstreams, tteobjects;

type

  { TForm4 }

  TForm4 = class(TForm)
    BitBtn1: TBitBtn;
    BitBtn2: TBitBtn;
    BitBtn3: TBitBtn;
    CheckBox1: TCheckBox;
    Edit1: TEdit;
    Edit2: TEdit;
    Edit3: TEdit;
    Edit4: TEdit;
    Edit5: TEdit;
    Image1: TImage;
    Image2: TImage;
    ImageList1: TImageList;
    Label1: TLabel;
    Label2: TLabel;
    Label3: TLabel;
    Label4: TLabel;
    Label5: TLabel;
    Label6: TLabel;
    Label7: TLabel;
    Label8: TLabel;
    ListView1: TListView;
    Panel1: TPanel;
    Panel2: TPanel;
    Panel3: TPanel;
    Panel4: TPanel;
    Panel5: TPanel;
    SpeedButton1: TSpeedButton;
    SpeedButton2: TSpeedButton;
    SpeedButton3: TSpeedButton;
    SpeedButton4: TSpeedButton;
    SpeedButton5: TSpeedButton;
    procedure BitBtn1Click(Sender: TObject);
    procedure BitBtn2Click(Sender: TObject);
    procedure BitBtn3Click(Sender: TObject);
    procedure Edit4Change(Sender: TObject);
    procedure Edit5Change(Sender: TObject);
    procedure FormCloseQuery(Sender: TObject; var CanClose: boolean);
    procedure FormCreate(Sender: TObject);
    procedure FormShow(Sender: TObject);
    procedure createpath(path: tstringlist; pdp1: String; pdp2: String; filename: String; humanname: String; player: boolean);
    procedure ListView1Click(Sender: TObject);
    procedure ListView1ColumnClick(Sender: TObject; Column: TListColumn);
    procedure ListView1DblClick(Sender: TObject);
    procedure Panel3Click(Sender: TObject);
    procedure Panel4Click(Sender: TObject);
    procedure SpeedButton1Click(Sender: TObject);
    procedure SpeedButton2Click(Sender: TObject);
    procedure SpeedButton3Click(Sender: TObject);
    procedure SpeedButton4Click(Sender: TObject);
    procedure ListView1Compare(Sender: TObject; Item1, Item2: TListItem; Data: Integer; var Compare: Integer);
    procedure SpeedButton5Click(Sender: TObject);

  private
    { private declarations }
  public
    { public declarations }
  end;

var
  Form4: TForm4;
  wItem: integer;
  ColumnToSort: Integer;
  LastSorted: Integer;
  SortDir: Integer;

resourceString
  rSids = 'Sidings';
  savepaths1 = 'Do you want to save the path with filename "';
  savepaths2 = '" and description "';
  rsave = 'Do you want to save the list?';
  irgendwas = 'irgendwas';

implementation

uses unit1;

{$R *.lfm}

{ TForm4 }

procedure tform4.createpath(path: tstringlist; pdp1: String; pdp2: String; filename: String; humanname: String; Player: boolean);
var fces: TCharEncStream;
begin
  path.Add('SIMISA@@@@@@@@@@JINX0P0t______');
  path.add('');
  path.add('Serial ( 1 )');
  path.add('TrackPDPs (');
  path.add(#9+'TrackPDP ( '+pdp1+' 1 0 )');
  path.add(#9+'TrackPDP ( '+pdp2+' 1 0 )');
  path.add(')');
  path.add('TrackPath (');
  path.add(#9+'TrPathName ( "'+filename+'" )');
  if player then path.add(#9+'TrPathFlags ( 00000000 )')
  else path.add(#9+'TrPathFlags ( 00000020 )');
  path.add(#9+'Name ( "'+humanname+'" )');
  path.add(#9+'TrPathStart ( "'+humanname+'" )');
  path.add(#9+'TrPathEnd ( "'+humanname+'" )');
  path.add(#9+'TrPathNodes ( 2');
  path.add(#9+#9+'TrPathNode ( 00000000 1 4294967295 0 )');
  path.add(#9+#9+'TrPathNode ( 00000000 4294967295 4294967295 1 )');
  path.add(#9+')');
  path.add(')');
  fces:=TCharEncStream.Create;
  fces.Reset;
  fces.HasBOM:=true;
  fces.HaveType:=true;
  fces.UniStreamType:=ufUtf16le;
  fces.UTF8Text:=path.text;
  fces.SaveToFile(UTF8ToSys(getroutepath+'paths\'+filename+'.pat'));
  fces.Free;
end;


procedure TForm4.ListView1Compare(Sender: TObject; Item1,
   Item2: TListItem; Data: Integer; var Compare: Integer);
var
   TempStr, TextToSort1, TextToSort2: String;
begin
//Texte zuweisen
   if ColumnToSort = 0 then
   begin
     TextToSort1 := Item1.Caption;
     TextToSort2 := Item2.Caption;
   end //if ColumnToSort = 0 then
   else
   begin
     TextToSort1 := Item1.SubItems[ColumnToSort - 1];
     TextToSort2 := Item2.SubItems[ColumnToSort - 1];
   end; //if ColumnToSort <> 0 then

//Je nach Sortierrichtung evtl. Texte vertauschen
   if SortDir <> 0 then
   begin
     TempStr := TextToSort1;
     TextToSort1 := TextToSort2;
     TextToSort2 := TempStr;
   end; //if SortDir <> 0 then

//Texte je nach Tag der Spalte unterschiedlich vergleichen
   case (Sender as TListView).Columns[ColumnToSort].Tag of
//Integer-Werte
     1: Compare := StrToIntDef(TextToSort1,0)-StrToIntDef(TextToSort2,0);
//Float-Werte
     2: begin
       Compare := 0;
       if StrToFloat(TextToSort1) > StrToFloat(TextToSort2) then
         Compare := Trunc(StrToFloat(TextToSort1)-StrToFloat(TextToSort2))+1;
       if StrToFloat(TextToSort1) < StrToFloat(TextToSort2) then
         Compare := Trunc(StrToFloat(TextToSort1)-StrToFloat(TextToSort2))-1;
     end; //2
//DateTime-Werte
     3: begin
       Compare := 0;
       if StrToDateTime(TextToSort1) > StrToDateTime(TextToSort2) then
         Compare := Trunc(StrToDateTime(TextToSort1)-StrToDateTime(TextToSort2))+1;
       if StrToDateTime(TextToSort1) < StrToDateTime(TextToSort2) then
         Compare := Trunc(StrToDateTime(TextToSort1)-StrToDateTime(TextToSort2))-1;
     end; //3
//Alles andere sind Strings
     else
       Compare := CompareText(TextToSort1,TextToSort2);
   end; //case (Sender as TListView).Columns[ColumnToSort].Tag of
end; //procedure TForm1.ListView1Compare

procedure TForm4.SpeedButton5Click(Sender: TObject);
var i: integer;
    //found: boolean;
   found: String;
begin
  for i:=0 to listview1.Items.Count -1 do begin
    //found:=false;
    //found:=testpathtosiding(listview1.items[i].Caption);
    //if found then listview1.items[i].SubItemImages[3]:=0 else listview1.items[i].SubItemImages[3]:=1;
    found:='';
    found:=testpathtosiding(listview1.items[i].caption);
    if found <> '' then begin
      listview1.items[i].subitemimages[3]:=0;
      listview1.items[i].subitems[4]:=found;
    end else begin
      listview1.items[i].subitemimages[3]:=1;
      listview1.items[i].subitems[4]:='';
    end;
  end;
end;

procedure TForm4.ListView1Click(Sender: TObject);
begin
  if listview1.ItemIndex >-1 then begin
    label6.caption:=listview1.Items[listview1.ItemIndex].SubItems[0];
    if listview1.Items[listview1.itemindex].subitems[1]<>'' then begin
      edit4.text:=listview1.items[listview1.itemindex].subitems[1];
    end else begin
      edit4.text:=listview1.items[listview1.itemindex].subitems[0];
    end;
    edit4.text:=Stringreplace(edit4.text,' ','_',[rfReplaceAll]);
    edit4.text:=Stringreplace(edit4.text,'/','_',[rfReplaceAll]);
    edit4.text:=Stringreplace(edit4.text,'\','_',[rfReplaceAll]);
    edit5.text:='';
    bitbtn3.Enabled:=false;
  end;
end;

procedure TForm4.ListView1ColumnClick(Sender: TObject; Column: TListColumn);
begin
  ColumnToSort := Column.Index;
   if ColumnToSort = LastSorted then
     SortDir := 1 - SortDir
   else
     SortDir := 0;
   LastSorted := ColumnToSort;
   (Sender as TCustomListView).AlphaSort;
end;

procedure TForm4.ListView1DblClick(Sender: TObject);
var li: TListItem;
begin
  wItem:=listview1.ItemIndex;
  li:=listview1.Items[listview1.ItemIndex];
  edit1.Text:=li.SubItems[0];
  if li.subitems.Count > 2 then begin
    edit2.text:=li.subitems[1];
    edit3.text:=li.subitems[2];
  end;
  panel4.visible:=true;
end;

procedure TForm4.Panel3Click(Sender: TObject);
begin

end;

procedure TForm4.Panel4Click(Sender: TObject);
begin

end;

procedure TForm4.SpeedButton1Click(Sender: TObject);
var fi: TSearchrec;
    tmp: tstringlist;
    fCES: TCHarEncStream;
    i,anz: integer;
    slist: TSidingobjectlist;
    li: TListItem;
    //tst: boolean;
    tst: String;
begin
  listview1.clear;
  if findfirst(getroutepath+'*.tdb',faAnyFile,fi) = 0 then begin
    tmp:=tstringlist.Create;
    FCES:=TCHarEncStream.Create;
    FCES.reset;
    fCES.LoadFromFile(UTF8ToSys(getroutepath+fi.name));
    tmp.text:=fces.utf8text;
    anz:=extractsidings(tmp);
    slist:=getSidings;
    for i:=0 to slist.count -1 do begin
      li:=listview1.items.add;
      li.caption:=slist.Items[i].itemid;
      li.SubItems.Add(slist.items[i].Name);
      li.subitems.add('');
      li.subitems.add('');
      tst:=testPathToSiding(li.caption);
      //if tst then li.subitems.add('x') else li.subitems.add('n');
      li.subitems.add('');
      if tst <> '' then begin
        li.SubItemImages[3]:=0;
        li.subitems.add(tst);
      end else begin
        li.SubItemImages[3]:=1;
        li.subitems.add('');
      end;
    end;
  end;
end;

procedure TForm4.SpeedButton2Click(Sender: TObject);
var i: integer;
    fces: TCharEncStream;
    Siding: TSiding;
    tmp,tmp2: tstringlist;
    li: tlistitem;
    //tst: boolean;
    tst: String;
begin
  if fileexists(getroutepath+'Activities\Openrails\sidings.siding') then begin
    tmp:=tstringlist.create;
    tmp2:=tstringlist.Create;
    fces:=TCharEncStream.create;
    FCES.reset;
    fCES.LoadFromFile(UTF8ToSys(getroutepath+'Activities\Openrails\sidings.siding'));
    tmp.text:=fces.utf8text;
    fces.Free;
    listview1.Items.Clear;
    clearsidingslist;
    for i:=1 to tmp.count -1 do begin
       split(';',tmp[i],tmp2);
       li:=listview1.Items.add;
       li.caption:=tmp2[0];
       li.subitems.add(tmp2[1]);
       li.subitems.add(tmp2[2]);
       li.subitems.add(tmp2[3]);
       Siding:=Tsiding.Create;
       siding.ItemId:=tmp2[0];
       siding.Name:=tmp2[1];
       siding.setItemRData(tmp2[4]+' '+tmp2[5]+' '+tmp2[6]+' '+tmp2[7]+' '+tmp2[8],1);
       siding.setItemRData(tmp2[9]+' '+tmp2[10]+' '+tmp2[11]+' '+tmp2[12]+' '+tmp2[13],2);
       addSidingtoList(siding);
       tst:=testPathToSiding(li.caption);
       li.subitems.add('');
       if tst <> '' then begin
         li.SubItemImages[3]:=0;
         li.SubItems.add(tst);
       end else begin
         li.SubItemImages[3]:=1;
         li.subitems.add('');
       end;
    end;
  end;
end;

procedure TForm4.SpeedButton3Click(Sender: TObject);
var i, n: integer;
    fCES: TCHarEncStream;
    slist: TSidingObjectList;
    Siding: TSiding;
    csv: tstringlist;
    tmp: String;
begin
  csv:=tstringlist.Create;
  csv.Add('ID;Name;IndividualName;Station;RDV11;RDV12;RDV13;RDV14;RDV15;RDV21;RDV22;RDV23;RDV24;RDV25');
  slist:=getsidings;
  for i:=0 to slist.count -1 do begin
    Siding:=slist[i];
    for n:=0 to listview1.Items.count -1 do begin
      if listview1.Items[n].Caption = siding.ItemId then begin
        tmp:=listview1.items[n].caption+';'+listview1.items[n].SubItems[0]+';'+listview1.items[n].subitems[1]+';'+listview1.items[n].subitems[2]+';';
        tmp:=tmp+siding.getRDataValue11()+';'+siding.getRDataValue12()+';'+siding.getRDataValue13()+';'+siding.getRDataValue14()+';'+siding.getRDataValue15()+';';
        tmp:=tmp+siding.getRDataValue21()+';'+siding.getRDataValue22()+';'+siding.getRDataValue23()+';'+siding.getRDataValue24()+';'+siding.getRDataValue25()+';';
        csv.add(tmp);
      end;
    end;
  end;
  fCES:=TCharEncStream.create;
  fCES.reset;
  fces.HasBOM:=true;
  fces.HaveType:=true;
  fces.UniStreamType:=ufUtf16le;
  fces.UTF8Text:=csv.text;
  fces.SaveToFile(UTF8ToSys(getroutepath+'Activities\Openrails\sidings.siding'));
  fces.Free;
end;

procedure TForm4.SpeedButton4Click(Sender: TObject);
begin
  edit5.text:=edit4.text;
  bitbtn3.enabled:=true;
end;

procedure TForm4.FormShow(Sender: TObject);
begin
  listview1.Align:=alClient;
  panel4.visible:=false;
  panel4.align:=alleft;
  panel5.width:=401;
  Label6.caption:='';
  bitbtn3.enabled:=false;
  if fileexists(getroutepath+'Activities\Openrails\sidings.siding') then speedbutton2.Click;
end;

procedure TForm4.BitBtn1Click(Sender: TObject);
begin
  if listview1.Items[witem].SubItems.count > 1 then begin
    listview1.Items[witem].subitems[1]:=edit2.text;
    listview1.items[witem].subitems[2]:=edit3.text;
  end else begin
    listview1.Items[witem].SubItems.Add(edit2.text);
    listview1.items[witem].subitems.add(edit3.text);
  end;
  panel4.visible:=false;
end;

procedure TForm4.BitBtn2Click(Sender: TObject);
begin
  panel4.visible:=false;
end;

procedure TForm4.BitBtn3Click(Sender: TObject);
var path: tstringlist;
    i: integer;
    fn, hn, pdp1, pdp2: String;
    sidings: TSidingObjectList;
    siding: TSiding;
begin
  sidings:=getSidings;
  pdp1:='';
  pdp2:='';
  for i:=0 to sidings.count -1 do begin
    if sidings[i].ItemId = listview1.Items[listview1.itemindex].Caption then begin
      siding:=sidings[i];
      pdp1:=siding.getRDataValue14()+' '+siding.getRDataValue15()+' '+siding.getRDataValue11()+' '+siding.getRDataValue12()+' '+siding.getRDataValue13() ;
      pdp2:=siding.getRDataValue24()+' '+siding.getRDataValue25()+' '+siding.getRDataValue21()+' '+siding.getRDataValue22()+' '+siding.getRDataValue23() ;
    end;
  end;
  if ( pdp1 <> '' ) and ( pdp2 <> '' ) then begin
    fn:='sid_'+edit4.text;
    if edit5.text = '' then hn:=edit4.text else hn:=edit5.text;
    if messagedlg(savepaths1+fn+savepaths2+hn+'"?', mtConfirmation, [mbyes,mbno],0) = mryes then begin
      path:=tstringlist.create;
      createpath(path,pdp1,pdp2,fn,hn,checkbox1.Checked);
      path:=tstringlist.create;
      createpath(path,pdp2,pdp1,fn+'_rev',hn+'_rev',checkbox1.Checked);
    end;
  end;
end;

procedure TForm4.Edit4Change(Sender: TObject);
begin
  if edit4.text <> '' then bitbtn3.enabled:=true;
end;

procedure TForm4.Edit5Change(Sender: TObject);
begin
  if edit5.text <> '' then bitbtn3.enabled:=true;
end;

procedure TForm4.FormCloseQuery(Sender: TObject; var CanClose: boolean);
begin
  if messagedlg(rsave,mtConfirmation,[mbyes,mbno],0) = mryes then speedbutton3.Click;
end;

procedure TForm4.FormCreate(Sender: TObject);
begin
  imagelist1.Add(image1.Picture.Bitmap,nil);
  imagelist1.add(image2.picture.bitmap,nil);
end;

end.

