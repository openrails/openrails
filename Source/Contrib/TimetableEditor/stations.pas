unit stations;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, ExtCtrls,
  StdCtrls, Buttons, LazUTF8, timetabledata, charencstreams;

type

  { TForm5 }

  TForm5 = class(TForm)
    BitBtn1: TBitBtn;
    BitBtn2: TBitBtn;
    BitBtn3: TBitBtn;
    BitBtn4: TBitBtn;
    ListBox1: TListBox;
    Memo1: TMemo;
    Panel1: TPanel;
    SaveDialog1: TSaveDialog;
    SpeedButton1: TSpeedButton;
    SpeedButton2: TSpeedButton;
    procedure BitBtn1Click(Sender: TObject);
    procedure BitBtn2Click(Sender: TObject);
    procedure BitBtn3Click(Sender: TObject);
    procedure BitBtn4Click(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure FormShow(Sender: TObject);
    procedure SpeedButton1Click(Sender: TObject);
    procedure SpeedButton2Click(Sender: TObject);
    procedure writestations();
  private
    { private declarations }
    fces: TCharEncStream;
  public
    { public declarations }
  end;

var
  Form5: TForm5;

resourceString
  irgendwas = 'irgendas';
  VrzNotExists = 'The stationfile has to be saved in "Activities\Openrails\" of the route. Should this folder be created?';
  savedlgtitle = 'save stations';

implementation
uses unit1;

{$R *.lfm}

{ TForm5 }

procedure TForm5.FormCreate(Sender: TObject);
begin
  memo1.Lines.Clear;
  memo1.left:=listbox1.left;
  memo1.top:=listbox1.top;
  memo1.Width:=250;
  memo1.height:=232;
  listbox1.Height:=232;
  listbox1.width:=250;
  listbox1.Visible:=false;
  bitbtn1.Visible:=false;
  bitbtn1.Top:=bitbtn2.top;
  bitbtn1.Width:=bitbtn2.width;
  bitbtn4.visible:=false;
end;

procedure TForm5.BitBtn3Click(Sender: TObject);  // Abbrechen
begin
  form5.ModalResult:=mrcancel;
end;

procedure TForm5.BitBtn4Click(Sender: TObject);    //Neu erstellen
begin
  Listbox1.Visible:=false;
  Bitbtn1.visible:=false;
  bitbtn2.visible:=true;
  panel1.visible:=true;
  form5.ActiveControl:=memo1;
end;

procedure TForm5.BitBtn2Click(Sender: TObject);   //Speichern und zurück
begin
  if not directoryexists(UTF8ToSys(getroutepath+'Activities\Openrails')) then begin
    //if messagedlg('Die Bahnhofsdateien müssen im Ordner "Activities\Openrails\" der Strecke gespeichert werden. Soll der Ordner erstellt werden?', mtConfirmation, [mbyes,mbno],0) = mryes then begin
    if messagedlg(VrzNotExists, mtConfirmation, [mbyes,mbno],0) = mryes then begin
      createdir(UTF8ToSys(getroutepath+'Activities\Openrails'));
    end;
  end;
  savedialog1.InitialDir:=UTF8ToSys(getroutepath+'Activities\Openrails');
  //savedialog1.Title:='Bahnhöfe speichern';
  savedialog1.Title:=savedlgtitle;
  //savedialog1.Filter:='Bahnhofsdateien|*.stations';
  savedialog1.Filter:='stationfiles (*.stations)|*.stations';
  savedialog1.FileName:=getroute+'.stations';
  if savedialog1.execute then begin
    fCES := TCharEncStream.Create;
    fCES.Reset;
    fCES.HasBOM:=true;
    fces.HaveType:=true;
    fCES.UniStreamType:=ufUtf16le;
    fCES.UTF8Text:=memo1.text;
    fces.SaveToFile(UTF8ToSys(savedialog1.FileName));
    fCes.Free;
    writestations;
  end;
  form5.modalresult:=mrok;
end;

procedure TForm5.BitBtn1Click(Sender: TObject);     // Auswählen
begin
  memo1.Lines:=loadstations(UTF8ToSys(getroutepath+'Activities\Openrails\'+listbox1.Items[Listbox1.itemindex]));
  writestations;
  form5.modalresult:=mrok;
end;

procedure TForm5.FormShow(Sender: TObject);
begin
  if getstationsfilescount > 0 then begin
    listbox1.Items:=getstationsfiles;
    listbox1.ItemIndex:=0;
    listbox1.Visible:=true;
    bitbtn2.visible:=false;
    bitbtn1.visible:=true;
    bitbtn4.visible:=true;
    panel1.visible:=false;
  end;
  memo1.Lines:=getstations;
end;

procedure TForm5.SpeedButton1Click(Sender: TObject);
var zeile: string;
begin
  if form5.ActiveControl=memo1 then begin
    if memo1.CaretPos.y > 0 then begin
      zeile:=memo1.Lines[memo1.caretpos.y];
      memo1.Lines.Delete(memo1.CaretPos.y);
      memo1.Lines.Insert(memo1.caretpos.y-1,zeile);
      memo1.CaretPos:=Point(0,memo1.CaretPos.y-1);
    end;
  end;
end;

procedure TForm5.SpeedButton2Click(Sender: TObject);
var zeile: string;
begin
  if form5.activecontrol=memo1 then begin
    if memo1.CaretPos.y<memo1.Lines.Count -1 then begin
      zeile:=memo1.lines[memo1.caretpos.y];
      memo1.lines.delete(memo1.caretpos.y);
      memo1.lines.insert(memo1.caretpos.y+1,zeile);
      memo1.caretpos:=point(0,memo1.caretpos.y-1);
    end;
  end;
end;

procedure tform5.writestations();
var i: integer;
begin
  form1.grid.cells[0,6]:='#note';
  if form1.grid.RowCount < memo1.Lines.Count + 12 then form1.grid.RowCount:= memo1.lines.count + 12;
  for i:= 0 to memo1.Lines.Count do begin
    form1.grid.cells[0,i+8]:=memo1.Lines[i];
  end;
  form1.grid.cells[0,memo1.lines.count +9]:='#dispose';
  form1.autosizecol(form1.grid, 0);
end;

end.

