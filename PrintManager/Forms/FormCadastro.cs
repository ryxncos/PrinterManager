using System;
using System.Drawing;
using System.Windows.Forms;
using PrintManager.Models;
using PrintManager.Services;

namespace PrintManager.Forms
{
    public class FormCadastro : Form
    {
        private readonly ImpressoraService _service = new ImpressoraService();
        private Impressora _impressora;
        private bool _modoEdicao;

        // ── Seção: Identificação ──────────────────────────────────────────────
        private TextBox txtNome;
        private TextBox txtSerie;
        private ComboBox cboTipo;

        // ── Seção: Localização ────────────────────────────────────────────────
        private TextBox txtIP;
        private TextBox txtSetor;
        private TextBox txtLocalizacao;

        // ── Peças ─────────────────────────────────────────────────────────────
        private (DateTimePicker Data, TextBox Descricao, TextBox Obs) _cabeca;
        private (DateTimePicker Data, TextBox Descricao, TextBox Obs) _roletes;
        private (DateTimePicker Data, TextBox Descricao, TextBox Obs) _recart;
        private (DateTimePicker Data, TextBox Descricao, TextBox Obs) _correias;
        private (DateTimePicker Data, TextBox Descricao, TextBox Obs) _sensores;

        // ── Checkboxes de "sem registro" ──────────────────────────────────────
        private CheckBox chkCabeca, chkRoletes, chkRecart, chkCorreias, chkSensores;

        public FormCadastro(Impressora impressora = null)
        {
            _modoEdicao = impressora != null;
            _impressora = impressora ?? new Impressora();
            InicializarComponentes();
            PreencherCampos();
        }

        private void InicializarComponentes()
        {
            Text = _modoEdicao ? "Editar impressora" : "Nova impressora";
            Size = new Size(780, 720);
            MinimumSize = new Size(680, 600);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 8)
            };

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ── Seções ────────────────────────────────────────────────────────
            layout.Controls.Add(CriarSecaoIdentificacao());
            layout.Controls.Add(CriarEspacador(6));
            layout.Controls.Add(CriarSecaoLocalizacao());
            layout.Controls.Add(CriarEspacador(6));
            layout.Controls.Add(CriarSecaoPecas());
            layout.Controls.Add(CriarEspacador(6));
            layout.Controls.Add(CriarRodape());

            scroll.Controls.Add(layout);
            Controls.Add(scroll);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SEÇÃO: IDENTIFICAÇÃO
        // ══════════════════════════════════════════════════════════════════════

        private GroupBox CriarSecaoIdentificacao()
        {
            var gb = CriarGroupBox("Identificação");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                Padding = new Padding(8)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            txtNome = new TextBox { Dock = DockStyle.Fill };
            txtSerie = new TextBox { Dock = DockStyle.Fill };
            cboTipo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTipo.Items.AddRange(new object[]
            {
                "TSC",
                "ZT410",
                "ZT411",
                "ZE511"
            });
            cboTipo.SelectedIndex = 0;

            panel.Controls.Add(CriarCampo("Nome da impressora *", txtNome), 0, 0);
            panel.Controls.Add(CriarCampo("Número de série", txtSerie), 1, 0);
            panel.Controls.Add(CriarCampo("Tipo *", cboTipo), 2, 0);

            gb.Controls.Add(panel);
            return gb;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SEÇÃO: LOCALIZAÇÃO
        // ══════════════════════════════════════════════════════════════════════

        private GroupBox CriarSecaoLocalizacao()
        {
            var gb = CriarGroupBox("Localização na rede");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                Padding = new Padding(8)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            txtIP = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5f) };
            txtSetor = new TextBox { Dock = DockStyle.Fill };
            txtLocalizacao = new TextBox { Dock = DockStyle.Fill };

            panel.Controls.Add(CriarCampo("Endereço IP", txtIP), 0, 0);
            panel.Controls.Add(CriarCampo("Setor / linha", txtSetor), 1, 0);
            panel.Controls.Add(CriarCampo("Localização física", txtLocalizacao), 0, 1);
            panel.SetColumnSpan(panel.GetControlFromPosition(0, 1).Parent ?? panel.Controls[panel.Controls.Count - 1], 2);

            gb.Controls.Add(panel);
            return gb;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SEÇÃO: PEÇAS
        // ══════════════════════════════════════════════════════════════════════

        private GroupBox CriarSecaoPecas()
        {
            var gb = CriarGroupBox("Peças e manutenção");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90));

            // Cria os 5 cards de peças
            _cabeca = CriarCardPeca(layout, "Cabeça de impressão", 0, 0, out chkCabeca);
            _roletes = CriarCardPeca(layout, "Roletes de pressão", 0, 1, out chkRoletes);
            _recart = CriarCardPeca(layout, "Recartilhado", 1, 0, out chkRecart);
            _correias = CriarCardPeca(layout, "Correias", 1, 1, out chkCorreias);

            // Sensores ocupa linha inteira
            var rowSensores = layout.RowCount;
            _sensores = CriarCardPecaLargura(layout, "Sensores de mídia", rowSensores, out chkSensores);

            gb.Controls.Add(layout);
            return gb;
        }

        private (DateTimePicker, TextBox, TextBox) CriarCardPeca(
            TableLayoutPanel parent, string titulo,
            int row, int col, out CheckBox chkSemData)
        {
            var panel = CriarPainelPeca(titulo, out var dtp, out var txtDesc, out var txtObs, out chkSemData);
            parent.Controls.Add(panel, col, row);
            return (dtp, txtDesc, txtObs);
        }

        private (DateTimePicker, TextBox, TextBox) CriarCardPecaLargura(
            TableLayoutPanel parent, string titulo, int row, out CheckBox chkSemData)
        {
            var panel = CriarPainelPeca(titulo, out var dtp, out var txtDesc, out var txtObs, out chkSemData);
            parent.Controls.Add(panel, 0, row);
            parent.SetColumnSpan(panel, 2);
            return (dtp, txtDesc, txtObs);
        }

        private Panel CriarPainelPeca(string titulo,
            out DateTimePicker dtp, out TextBox txtDesc,
            out TextBox txtObs, out CheckBox chkSemData)
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4),
                Margin = new Padding(4)
            };

            // Cabeçalho colorido
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(230, 241, 251),
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8, 4, 10, 4)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var lblTitulo = new Label
            {
                Text = titulo,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(12, 68, 124),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoEllipsis = true
            };

            chkSemData = new CheckBox
            {
                Text = "Sem registro",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                ForeColor = Color.FromArgb(24, 95, 165),
                Font = new Font("Segoe UI", 8.5f),
                Margin = new Padding(8, 0, 0, 0)
            };

            header.Controls.Add(lblTitulo, 0, 0);
            header.Controls.Add(chkSemData, 1, 0);

            // Corpo
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                Padding = new Padding(6)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            dtp = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            txtDesc = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Descrição..." };
            txtObs = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Observações...", Multiline = true, Height = 42 };

            // Checkbox "Sem registro" desabilita o DateTimePicker
            var dtpLocal = dtp;
            chkSemData.CheckedChanged += (s, e) => dtpLocal.Enabled = !((CheckBox)s).Checked;

            body.Controls.Add(CriarCampo("Última substituição", dtp), 0, 0);
            body.Controls.Add(CriarCampo("Descrição", txtDesc), 1, 0);

            var obsPanel = CriarCampo("Observações", txtObs);
            body.Controls.Add(obsPanel, 0, 1);
            body.SetColumnSpan(obsPanel, 2);

            var border = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 220, 220),
                Padding = new Padding(1)
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = SystemColors.Window };
            inner.Controls.Add(body);
            inner.Controls.Add(header);
            border.Controls.Add(inner);
            outer.Controls.Add(border);

            return outer;
        }

        // ══════════════════════════════════════════════════════════════════════
        // RODAPÉ
        // ══════════════════════════════════════════════════════════════════════

        private Panel CriarRodape()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Height = 44 };

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Width = 100,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Left = 0,
                Top = 4
            };
            btnCancelar.FlatAppearance.BorderColor = Color.Silver;
            btnCancelar.Click += (s, e) => Close();

            var btnSalvar = new Button
            {
                Text = "Salvar impressora",
                Width = 150,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 95, 165),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Left = 108,
                Top = 4
            };
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.Click += (s, e) => Salvar();

            panel.Controls.Add(btnCancelar);
            panel.Controls.Add(btnSalvar);

            // Alinha à direita dinamicamente
            panel.Resize += (s, e) =>
            {
                btnSalvar.Left = panel.Width - btnSalvar.Width - 8;
                btnCancelar.Left = btnSalvar.Left - btnCancelar.Width - 8;
            };

            return panel;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PREENCHER / SALVAR
        // ══════════════════════════════════════════════════════════════════════

        private void PreencherCampos()
        {
            // Identificação
            txtNome.Text = _impressora.Nome ?? "";
            txtSerie.Text = _impressora.NumeroSerie ?? "";
            cboTipo.SelectedIndex = (int)_impressora.Tipo;

            // Localização
            txtIP.Text = _impressora.EnderecoIP ?? "";
            txtSetor.Text = _impressora.Setor ?? "";
            txtLocalizacao.Text = _impressora.Localizacao ?? "";

            // Peças
            PreencherPeca(_cabeca, chkCabeca, _impressora.CabecaImpressao);
            PreencherPeca(_roletes, chkRoletes, _impressora.RoletesPressao);
            PreencherPeca(_recart, chkRecart, _impressora.Recartilhado);
            PreencherPeca(_correias, chkCorreias, _impressora.Correias);
            PreencherPeca(_sensores, chkSensores, _impressora.SensoresMidia);
        }

        private void PreencherPeca(
            (DateTimePicker Data, TextBox Descricao, TextBox Obs) campos,
            CheckBox chk, Peca peca)
        {
            if (peca == null) return;

            if (peca.UltimaSubstituicao.HasValue)
            {
                campos.Data.Value = peca.UltimaSubstituicao.Value;
                chk.Checked = false;
                campos.Data.Enabled = true;
            }
            else
            {
                chk.Checked = true;
                campos.Data.Enabled = false;
            }

            campos.Descricao.Text = peca.Descricao ?? "";
            campos.Obs.Text = peca.Observacoes ?? "";
        }

        private void Salvar()
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("O campo Nome é obrigatório.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNome.Focus();
                return;
            }

            // Identificação
            _impressora.Nome = txtNome.Text.Trim();
            _impressora.NumeroSerie = txtSerie.Text.Trim();
            _impressora.Tipo = (TipoImpressora)cboTipo.SelectedIndex;

            // Localização
            _impressora.EnderecoIP = txtIP.Text.Trim();
            _impressora.Setor = txtSetor.Text.Trim();
            _impressora.Localizacao = txtLocalizacao.Text.Trim();

            // Peças
            ColetarPeca(_cabeca, chkCabeca, _impressora.CabecaImpressao);
            ColetarPeca(_roletes, chkRoletes, _impressora.RoletesPressao);
            ColetarPeca(_recart, chkRecart, _impressora.Recartilhado);
            ColetarPeca(_correias, chkCorreias, _impressora.Correias);
            ColetarPeca(_sensores, chkSensores, _impressora.SensoresMidia);

            _service.Salvar(_impressora);

            MessageBox.Show(
                _modoEdicao ? "Impressora atualizada com sucesso!" : "Impressora cadastrada com sucesso!",
                "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ColetarPeca(
            (DateTimePicker Data, TextBox Descricao, TextBox Obs) campos,
            CheckBox chk, Peca peca)
        {
            peca.UltimaSubstituicao = chk.Checked ? null : campos.Data.Value;
            peca.Descricao = campos.Descricao.Text.Trim();
            peca.Observacoes = campos.Obs.Text.Trim();
        }

        // ══════════════════════════════════════════════════════════════════════
        // UTILITÁRIOS DE LAYOUT
        // ══════════════════════════════════════════════════════════════════════

        private GroupBox CriarGroupBox(string titulo)
        {
            return new GroupBox
            {
                Text = titulo,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(4, 8, 4, 8)
            };
        }

        private Panel CriarCampo(string label, Control controle)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Padding = new Padding(4, 2, 4, 6)
            };

            var lbl = new Label
            {
                Text = label,
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Color.FromArgb(90, 90, 90),
                Font = new Font("Segoe UI", 8.5f)
            };

            controle.Dock = DockStyle.Fill;
            panel.Controls.Add(controle);
            panel.Controls.Add(lbl);
            return panel;
        }

        private void InitializeComponent()
        {

        }

        private Panel CriarEspacador(int altura)
        {
            return new Panel { Dock = DockStyle.Fill, Height = altura };
        }
    }
}
