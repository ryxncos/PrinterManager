using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PrintManager.Models;
using PrintManager.Services;

namespace PrintManager.Forms
{
    public class FormLista : Form
    {
        // Serviços
        private readonly ImpressoraService _service = new ImpressoraService();
        private readonly PingService _ping = new PingService();

        // Controles da toolbar
        private TextBox txtBusca;
        private Button btnNova;
        private Button btnPingTodos;

        // Grade principal
        private DataGridView grid;

        // Status bar
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblTotal;
        private ToolStripStatusLabel lblOnline;
        private ToolStripStatusLabel lblOffline;

        public FormLista()
        {
            InicializarComponentes();
            CarregarDados();
        }

        private void InicializarComponentes()
        {
            // --- Janela principal ---
            Text = "Gerenciamento de Impressoras";
            Size = new Size(1100, 600);
            MinimumSize = new Size(900, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            // --- Toolbar ---
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8, 8, 8, 0),
                BackColor = SystemColors.Control
            };

            txtBusca = new TextBox
            {
                PlaceholderText = "Buscar por nome, setor, IP ou tipo...",
                Width = 320,
                Height = 28,
                Left = 8,
                Top = 10,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtBusca.TextChanged += (s, e) => AtualizarGrid(_service.Filtrar(txtBusca.Text));

            btnPingTodos = new Button
            {
                Text = "⟳  Ping todos",
                Width = 110,
                Height = 28,
                Left = txtBusca.Right + 8,
                Top = 10,
                FlatStyle = FlatStyle.Flat
            };
            btnPingTodos.FlatAppearance.BorderColor = Color.Silver;
            btnPingTodos.Click += async (s, e) => await PingTodosAsync();

            btnNova = new Button
            {
                Text = "+  Nova impressora",
                Width = 140,
                Height = 28,
                Left = btnPingTodos.Right + 8,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 95, 165),
                ForeColor = Color.White
            };
            btnNova.FlatAppearance.BorderSize = 0;
            btnNova.Click += (s, e) => AbrirFormCadastro(null);

            toolbar.Controls.AddRange(new Control[] { txtBusca, btnPingTodos, btnNova });

            // --- DataGridView ---
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                BorderStyle = BorderStyle.None,
                BackgroundColor = SystemColors.Window,
                GridColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Segoe UI", 9.5f),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 36 }
            };

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
            grid.EnableHeadersVisualStyles = false;

            ConfigurarColunas();
            grid.CellFormatting += Grid_CellFormatting;
            grid.CellContentClick += Grid_CellContentClick;
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) EditarSelecionada();
            };

            // --- Status bar ---
            statusBar = new StatusStrip();
            lblOnline = new ToolStripStatusLabel("● 0 online") { ForeColor = Color.Green };
            lblOffline = new ToolStripStatusLabel("● 0 offline") { ForeColor = Color.Red };
            lblTotal = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight };
            statusBar.Items.AddRange(new ToolStripItem[] { lblOnline, new ToolStripSeparator(), lblOffline, lblTotal });

            // --- Montar layout ---
            Controls.Add(grid);
            Controls.Add(toolbar);
            Controls.Add(statusBar);
        }

        private void ConfigurarColunas()
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "Status",
                Width = 90,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNome",
                HeaderText = "Nome",
                Width = 160,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTipo",
                HeaderText = "Tipo",
                Width = 140,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSetor",
                HeaderText = "Setor",
                Width = 120,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colIP",
                HeaderText = "Endereço IP",
                Width = 120,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPecaAntiga",
                HeaderText = "Peça mais antiga",
                Width = 200,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colUltModif",
                HeaderText = "Última modificação",
                Width = 150,
                ReadOnly = true
            });

            // Coluna de ações com botões
            var colAcoes = new DataGridViewTextBoxColumn
            {
                Name = "colAcoes",
                HeaderText = "Ações",
                Width = 120,
                ReadOnly = true
            };
            grid.Columns.Add(colAcoes);

            // Botão Ping individual
            var colPing = new DataGridViewButtonColumn
            {
                Name = "colPing",
                HeaderText = "",
                Width = 36,
                Text = "⇄",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(colPing);

            // Botão Editar
            var colEditar = new DataGridViewButtonColumn
            {
                Name = "colEditar",
                HeaderText = "",
                Width = 36,
                Text = "✎",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(colEditar);

            // Botão Excluir
            var colExcluir = new DataGridViewButtonColumn
            {
                Name = "colExcluir",
                HeaderText = "",
                Width = 36,
                Text = "✕",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(colExcluir);

            // Remove coluna de ações texto (usamos os botões)
            grid.Columns.Remove("colAcoes");
        }

        // ─── Dados ────────────────────────────────────────────────────────────

        private void CarregarDados()
        {
            AtualizarGrid(_service.ListarTodas());
        }

        private void AtualizarGrid(List<Impressora> lista)
        {
            grid.Rows.Clear();
            int online = 0, offline = 0;

            foreach (var imp in lista)
            {
                var peca = imp.PecaMaisAntiga();
                var pecaTxt = peca?.UltimaSubstituicao.HasValue == true
                    ? $"{peca.Nome} — {peca.UltimaSubstituicao:dd/MM/yyyy}"
                    : peca?.Nome != null ? $"{peca.Nome} — sem registro" : "—";

                var modifTxt = imp.UltimaModificacaoGeral.HasValue
                    ? FormatarData(imp.UltimaModificacaoGeral.Value)
                    : "—";

                // Tag de status — será atualizada pelo ping; inicia como "?"
                var idx = grid.Rows.Add(
                    "?",
                    imp.Nome,
                    FormatarTipo(imp.Tipo),
                    imp.Setor ?? "—",
                    imp.EnderecoIP ?? "—",
                    pecaTxt,
                    modifTxt
                );

                grid.Rows[idx].Tag = imp.Id;  // guarda o Id para operações
            }

            lblTotal.Text = $"{lista.Count} impressora(s) cadastrada(s)";
            AtualizarContadores(online, offline);
        }

        // ─── Ping ─────────────────────────────────────────────────────────────

        private async Task PingTodosAsync()
        {
            btnPingTodos.Enabled = false;
            btnPingTodos.Text = "Verificando...";
            int online = 0, offline = 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                var id = (Guid)row.Tag;
                var imp = _service.BuscarPorId(id);
                if (imp == null) continue;

                var resultado = await _ping.TestarDetalhesAsync(imp.EnderecoIP);
                row.Cells["colStatus"].Value = resultado.Online ? "Online" : "Offline";

                if (resultado.Online) online++;
                else offline++;
            }

            AtualizarContadores(online, offline);
            btnPingTodos.Enabled = true;
            btnPingTodos.Text = "⟳  Ping todos";
        }

        private async Task PingIndividualAsync(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[rowIndex];
            var id = (Guid)row.Tag;
            var imp = _service.BuscarPorId(id);
            if (imp == null) return;

            row.Cells["colStatus"].Value = "...";
            var resultado = await _ping.TestarDetalhesAsync(imp.EnderecoIP);
            row.Cells["colStatus"].Value = resultado.Online ? "Online" : "Offline";
        }

        // ─── Formatação visual ────────────────────────────────────────────────

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (grid.Columns[e.ColumnIndex].Name == "colStatus")
            {
                var val = e.Value?.ToString();
                e.CellStyle.ForeColor = val == "Online"
                    ? Color.FromArgb(39, 80, 10)
                    : val == "Offline"
                        ? Color.FromArgb(121, 31, 31)
                        : Color.Gray;
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }

            if (grid.Columns[e.ColumnIndex].Name == "colIP")
            {
                e.CellStyle.Font = new Font("Consolas", 9f);
                e.CellStyle.ForeColor = Color.FromArgb(60, 60, 60);
            }
        }

        // ─── Ações dos botões na grade ────────────────────────────────────────

        private async void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            switch (grid.Columns[e.ColumnIndex].Name)
            {
                case "colPing":
                    await PingIndividualAsync(e.RowIndex);
                    break;

                case "colEditar":
                    EditarSelecionada();
                    break;

                case "colExcluir":
                    ExcluirSelecionada();
                    break;
            }
        }

        private void EditarSelecionada()
        {
            if (grid.CurrentRow == null) return;
            var id = (Guid)grid.CurrentRow.Tag;
            var imp = _service.BuscarPorId(id);
            AbrirFormCadastro(imp);
        }

        private void ExcluirSelecionada()
        {
            if (grid.CurrentRow == null) return;
            var id = (Guid)grid.CurrentRow.Tag;
            var imp = _service.BuscarPorId(id);
            if (imp == null) return;

            var confirmar = MessageBox.Show(
                $"Deseja excluir a impressora \"{imp.Nome}\"?",
                "Confirmar exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirmar == DialogResult.Yes)
            {
                _service.Remover(id);
                CarregarDados();
            }
        }

        private void AbrirFormCadastro(Impressora impressora)
        {
            using var form = new FormCadastro(impressora);
            if (form.ShowDialog() == DialogResult.OK)
                CarregarDados();
        }

        // ─── Utilitários ──────────────────────────────────────────────────────

        private void AtualizarContadores(int online, int offline)
        {
            lblOnline.Text = $"● {online} online";
            lblOffline.Text = $"● {offline} offline";
        }

        private string FormatarData(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "agora";
            if (diff.TotalHours < 1) return $"há {(int)diff.TotalMinutes} min";
            if (diff.TotalDays < 1) return "hoje";
            if (diff.TotalDays < 2) return "ontem";
            if (diff.TotalDays < 7) return $"há {(int)diff.TotalDays} dias";
            return dt.ToString("dd/MM/yyyy");
        }

        private void InitializeComponent()
        {

        }

        private string FormatarTipo(TipoImpressora tipo) => tipo switch
        {
            TipoImpressora.TSC => "TSC",
            TipoImpressora.ZT410 => "ZT410",
            TipoImpressora.ZT411 => "ZT411",
            TipoImpressora.ZE511 => "ZE511",
            _ => tipo.ToString()
        };
    }
}
