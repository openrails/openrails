unit about;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, StdCtrls,lclintf;

type

  { TForm2 }

  TForm2 = class(TForm)
    Button1: TButton;
    Label1: TLabel;
    Label2: TLabel;
    StaticText1: TStaticText;
    procedure Button1Click(Sender: TObject);
    procedure StaticText4Click(Sender: TObject);

  private
    { private declarations }
  public
    { public declarations }
  end;

var
  Form2: TForm2;

resourceString
  irgendwas = 'irgendas';

implementation

{$R *.lfm}

{ TForm2 }

procedure TForm2.Button1Click(Sender: TObject);
begin
  form2.Close;
end;


procedure TForm2.StaticText4Click(Sender: TObject);
begin

end;


end.

