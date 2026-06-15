using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PrintManager.Models;
using PrintManager.Services;

namespace PrintManager.Forms
{
    public class FormHistorico : Form
    {
        private readonly ImpressoraService _service = new ImpressoraService();

        // Toolbar
        private ComboBox cboImpressora;

        // Cards de resumo
        private Label lblTotalPecas;
        private Label lblComRegistro;
        private Label lblMaisAntiga;

        // Layout central
        private ListBox lstPecas;
        private Panel painelTimeline;

        private Impressora _impressoraAtual;

        public FormHistorico(Impressora impressoraInicial = null)
        {
            InicializarComponentes();
            PopularCombo();
            if (impressoraInicial != null)
                SelecionarImpressora(impressoraInicial);
            else if (cboImpressora.Items.Count > 0)
                cboImpressora.SelectedIndex = 0;
        }

        private void InicializarComponentes()
        {
            Text = "Histórico de manutenção";
            Size = new Size(860, 620);
            MinimumSize = new Size(700, 500);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(12, 8, 12, 0),
                BackColor = SystemColors.Control
            };

            var lblSel = new Label
            {
                Text = "Impressora:",
                AutoSize = true,
                Top = 12, Left = 0,
                ForeColor = Color.FromArgb(90, 90, 90)
            };

            cboImpressora = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 380,
                Top = 8,
                Left = lblSel.Right + 6,
                Font = new Font("Segoe UI", 9.5f)
            };
            cboImpressora.SelectedIndexChanged += (s, e) => CarregarImpressora();

            toolbar.Controls.Add(lblSel);
            toolbar.Controls.Add(cboImpressora);

            // ── Cards de resumo ───────────────────────────────────────────────
            var painelResumo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(12, 8, 12, 0)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));

            lblTotalPecas  = new Label { Font = new Font("Segoe UI", 9f) };
            lblComRegistro = new Label { Font = new Font("Segoe UI", 9f) };
            lblMaisAntiga  = new Label { Font = new Font("Segoe UI", 9f) };

            tbl.Controls.Add(CriarCardResumo("Peças monitoradas", lblTotalPecas), 0, 0);
            tbl.Controls.Add(CriarCardResumo("Com registro de data", lblComRegistro), 1, 0);
            tbl.Controls.Add(CriarCardResumo("Peça mais antiga", lblMaisAntiga), 2, 0);

            painelResumo.Controls.Add(tbl);

            // ── Área central: sidebar + timeline ──────────────────────────────
            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 210,
                Panel1MinSize = 160,
                Panel2MinSize = 300,
                BorderStyle = BorderStyle.None
            };

            // Sidebar — lista de peças
            var lblPecas = new Label
            {
                Text = "PEÇAS",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(110, 110, 110),
                BackColor = SystemColors.Control
            };

            lstPecas = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = 44,
                DrawMode = DrawMode.OwnerDrawFixed,
                SelectionMode = SelectionMode.One
            };
            lstPecas.DrawItem += ListaPecas_DrawItem;
            lstPecas.SelectedIndexChanged += (s, e) => AtualizarTimeline();

            splitter.Panel1.Controls.Add(lstPecas);
            splitter.Panel1.Controls.Add(lblPecas);
            splitter.Panel1.BackColor = SystemColors.Control;
            splitter.Panel1.BorderStyle = BorderStyle.None;

            // Painel da timeline (direita)
            painelTimeline = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = SystemColors.Window
            };

            splitter.Panel2.Controls.Add(painelTimeline);

            // ── Montar layout ─────────────────────────────────────────────────
            Controls.Add(splitter);
            Controls.Add(painelResumo);
            Controls.Add(toolbar);
        }

        // ══════════════════════════════════════════════════════════════════════
        // POPULANDO DADOS
        // ══════════════════════════════════════════════════════════════════════

        private void PopularCombo()
        {
            cboImpressora.Items.Clear();
            foreach (var imp in _service.ListarTodas())
            {
                cboImpressora.Items.Add(new ImpressoraItem(imp));
            }
        }

        private void SelecionarImpressora(Impressora imp)
        {
            for (int i = 0; i < cboImpressora.Items.Count; i++)
            {
                if (((ImpressoraItem)cboImpressora.Items[i]).Impressora.Id == imp.Id)
                {
                    cboImpressora.SelectedIndex = i;
                    return;
                }
            }
        }

        private void CarregarImpressora()
        {
            if (cboImpressora.SelectedItem == null) return;
            _impressoraAtual = ((ImpressoraItem)cboImpressora.SelectedItem).Impressora;

            AtualizarResumo();
            PopularListaPecas();
        }

        private void AtualizarResumo()
        {
            if (_impressoraAtual == null) return;

            var pecas = ObterTodasPecas(_impressoraAtual);
            int comData = 0;
            Peca maisAntiga = null;

            foreach (var p in pecas)
            {
                if (p.UltimaSubstituicao.HasValue)
                {
                    comData++;
                    if (maisAntiga == null || p.MaisAntigaQue(maisAntiga))
                        maisAntiga = p;
                }
            }

            lblTotalPecas.Text  = pecas.Count.ToString();
            lblComRegistro.Text = comData.ToString();
            lblMaisAntiga.Text  = maisAntiga != null
                ? $"{maisAntiga.Nome}"
                : "—";
            lblMaisAntiga.ForeColor = maisAntiga != null
                ? Color.FromArgb(192, 80, 0)
                : Color.FromArgb(90, 90, 90);
        }

        private void PopularListaPecas()
        {
            lstPecas.Items.Clear();
            if (_impressoraAtual == null) return;

            var maisAntiga = _impressoraAtual.PecaMaisAntiga();

            foreach (var peca in ObterTodasPecas(_impressoraAtual))
            {
                lstPecas.Items.Add(new PecaItem(peca, peca == maisAntiga));
            }

            if (lstPecas.Items.Count > 0)
                lstPecas.SelectedIndex = 0;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TIMELINE
        // ══════════════════════════════════════════════════════════════════════

        private void AtualizarTimeline()
        {
            painelTimeline.Controls.Clear();

            if (_impressoraAtual == null || lstPecas.SelectedItem == null) return;

            var item = (PecaItem)lstPecas.SelectedItem;
            var peca = item.Peca;

            // Cabeçalho da timeline
            var lblTitulo = new Label
            {
                Text = peca.Nome,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            var lblSub = new Label
            {
                Text = $"{_impressoraAtual.Nome}  ·  {FormatarTipo(_impressoraAtual.Tipo)}  ·  {_impressoraAtual.Setor ?? "—"}",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            var separador = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(220, 220, 220),
                Margin = new Padding(0, 6, 0, 10)
            };

            // Painel da linha do tempo
            var timelinePanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(20, 8, 0, 0)
            };

            var itens = new List<Control>();

            // Entrada de substituição (se existir)
            if (peca.UltimaSubstituicao.HasValue)
            {
                itens.Add(CriarEntradaTimeline(
                    peca.UltimaSubstituicao.Value.ToString("dd/MM/yyyy") + " — último registro",
                    "Substituição registrada",
                    peca.Descricao,
                    peca.Observacoes,
                    destaque: true
                ));
            }
            else
            {
                itens.Add(CriarEntradaSemRegistro());
            }

            // Última modificação geral
            if (_impressoraAtual.UltimaModificacaoGeral.HasValue)
            {
                itens.Add(CriarEntradaTimeline(
                    "Modificação geral: " + FormatarData(_impressoraAtual.UltimaModificacaoGeral.Value),
                    "Última modificação da impressora",
                    "Registro atualizado pelo sistema",
                    null,
                    destaque: false
                ));
            }

            // Adiciona de baixo para cima (Dock.Top empilha invertido)
            for (int i = itens.Count - 1; i >= 0; i--)
                timelinePanel.Controls.Add(itens[i]);

            // Empilha no painel principal (de baixo para cima)
            painelTimeline.Controls.Add(timelinePanel);
            painelTimeline.Controls.Add(separador);
            painelTimeline.Controls.Add(lblSub);
            painelTimeline.Controls.Add(lblTitulo);
        }

        private Panel CriarEntradaTimeline(
            string data, string titulo, string descricao,
            string observacoes, bool destaque)
        {
            var outer = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 14)
            };

            // Linha da data
            var lblData = new Label
            {
                Text = data,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(120, 120, 120)
            };

            // Card
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = destaque ? Color.White : Color.FromArgb(247, 247, 247),
                Padding = new Padding(12, 10, 12, 10)
            };

            var borda = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.FromArgb(200, 200, 200),
                Padding = new Padding(1)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = destaque
                    ? Color.FromArgb(20, 20, 20)
                    : Color.FromArgb(100, 100, 100)
            };
            layout.Controls.Add(lblTitulo);

            if (!string.IsNullOrWhiteSpace(descricao))
            {
                var lblDesc = new Label
                {
                    Text = descricao,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(90, 90, 90),
                    Padding = new Padding(0, 3, 0, 0)
                };
                layout.Controls.Add(lblDesc);
            }

            if (!string.IsNullOrWhiteSpace(observacoes))
            {
                var sep = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 1,
                    BackColor = Color.FromArgb(220, 220, 220),
                    Margin = new Padding(0, 5, 0, 5)
                };
                var lblObs = new Label
                {
                    Text = "Obs: " + observacoes,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(110, 110, 110),
                    Padding = new Padding(0, 4, 0, 0)
                };
                layout.Controls.Add(sep);
                layout.Controls.Add(lblObs);
            }

            card.Controls.Add(layout);
            borda.Controls.Add(card);

            outer.Controls.Add(borda);
            outer.Controls.Add(lblData);
            return outer;
        }

        private Panel CriarEntradaSemRegistro()
        {
            var outer = new Panel { Dock = DockStyle.Top, Height = 60 };
            var lbl = new Label
            {
                Text = "Nenhuma substituição registrada para esta peça.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            outer.Controls.Add(lbl);
            return outer;
        }

        // ══════════════════════════════════════════════════════════════════════
        // DESENHO DA LISTA DE PEÇAS (owner draw)
        // ══════════════════════════════════════════════════════════════════════

        private void ListaPecas_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstPecas.Items.Count) return;

            var item = (PecaItem)lstPecas.Items[e.Index];
            var peca = item.Peca;

            bool selecionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            var bgColor = selecionado
                ? Color.FromArgb(230, 241, 251)
                : SystemColors.Window;

            e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);

            // Barra lateral de destaque
            if (selecionado)
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(24, 95, 165)),
                    new Rectangle(e.Bounds.Left, e.Bounds.Top, 3, e.Bounds.Height));

            // Nome da peça
            var nomeFont = new Font("Segoe UI", 9.5f, selecionado ? FontStyle.Bold : FontStyle.Regular);
            e.Graphics.DrawString(peca.Nome, nomeFont,
                new SolidBrush(Color.FromArgb(30, 30, 30)),
                new PointF(e.Bounds.Left + 12, e.Bounds.Top + 8));

            // Data ou aviso
            string sub;
            Color corSub;
            if (peca.UltimaSubstituicao.HasValue)
            {
                sub = item.EhMaisAntiga
                    ? $"⚠ Mais antiga: {peca.UltimaSubstituicao:dd/MM/yyyy}"
                    : $"Última troca: {peca.UltimaSubstituicao:dd/MM/yyyy}";
                corSub = item.EhMaisAntiga
                    ? Color.FromArgb(192, 80, 0)
                    : Color.FromArgb(100, 100, 100);
            }
            else
            {
                sub = "Sem registro";
                corSub = Color.FromArgb(150, 150, 150);
            }

            e.Graphics.DrawString(sub, new Font("Segoe UI", 8.5f),
                new SolidBrush(corSub),
                new PointF(e.Bounds.Left + 12, e.Bounds.Top + 26));

            // Linha divisória
            e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 220)),
                e.Bounds.Left, e.Bounds.Bottom - 1,
                e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        // ══════════════════════════════════════════════════════════════════════
        // UTILITÁRIOS
        // ══════════════════════════════════════════════════════════════════════

        private List<Peca> ObterTodasPecas(Impressora imp) => new List<Peca>
        {
            imp.CabecaImpressao,
            imp.RoletesPressao,
            imp.Recartilhado,
            imp.Correias,
            imp.SensoresMidia
        };

        private Panel CriarCardResumo(string label, Label lblValor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = SystemColors.Control,
                Padding = new Padding(10, 6, 10, 6)
            };

            var borda = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(210, 210, 210),
                Padding = new Padding(1)
            };

            var inner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                Padding = new Padding(10, 8, 10, 8)
            };

            lblValor.Text = "—";
            lblValor.Dock = DockStyle.Top;
            lblValor.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblValor.ForeColor = Color.FromArgb(30, 30, 30);
            lblValor.Height = 28;

            var lblLegenda = new Label
            {
                Text = label,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(110, 110, 110)
            };

            inner.Controls.Add(lblLegenda);
            inner.Controls.Add(lblValor);
            borda.Controls.Add(inner);
            card.Controls.Add(borda);
            return card;
        }

        private string FormatarData(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1)  return "agora";
            if (diff.TotalHours   < 1)  return $"há {(int)diff.TotalMinutes} min";
            if (diff.TotalDays    < 1)  return "hoje";
            if (diff.TotalDays    < 2)  return "ontem";
            if (diff.TotalDays    < 7)  return $"há {(int)diff.TotalDays} dias";
            return dt.ToString("dd/MM/yyyy");
        }

        private string FormatarTipo(TipoImpressora tipo) => tipo switch
        {
            TipoImpressora.TSC               => "TSC",
            TipoImpressora.ZT410 => "ZT410",
            TipoImpressora.ZT411 => "ZT411",
            TipoImpressora.ZE511       => "Zebra ZE511",
            _ => tipo.ToString()
        };

        // ══════════════════════════════════════════════════════════════════════
        // CLASSES AUXILIARES
        // ══════════════════════════════════════════════════════════════════════

        private class ImpressoraItem
        {
            public Impressora Impressora { get; }
            public ImpressoraItem(Impressora imp) => Impressora = imp;
            public override string ToString()
            {
                var tipo = Impressora.Tipo switch
                {
                    TipoImpressora.TSC => "TSC",
                    TipoImpressora.ZT410 => "ZT410",
                    TipoImpressora.ZT411 => "ZT411",
                    TipoImpressora.ZE511 => "Zebra ZE511",
                    _ => Impressora.Tipo.ToString()
                };
                return $"{Impressora.Nome}  —  {tipo}  —  {Impressora.Setor ?? "sem setor"}";
            }
        }

        private class PecaItem
        {
            public Peca Peca { get; }
            public bool EhMaisAntiga { get; }
            public PecaItem(Peca peca, bool ehMaisAntiga)
            {
                Peca = peca;
                EhMaisAntiga = ehMaisAntiga;
            }
            public override string ToString() => Peca.Nome;
        }
    }
}
