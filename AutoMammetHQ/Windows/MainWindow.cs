using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoMammetHQ.Data;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AutoMammetHQ.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Reader reader;
    private readonly Plugin plugin;
    private readonly Configuration config;

    private Handicraft[] handicrafts;
    private ScheduleHandler scheduleHandler;
    private IEnumerable<Schedule>? schedules = null;

    public MainWindow(Plugin plugin, Reader reader, string handicraftJsonPath) : base(
        "AutoMammetHQ - Main Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 50),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.reader = reader;
        this.plugin = plugin;
        this.config = plugin.Configuration;
    }

    public void Dispose()
    {
        Dalamud.ClientState.ClientLanguage.ToString();
    }

    public override void Draw()
    {
        if (!reader.IsSupplyAndDemandAvailable())
        {
            ImGui.Text("Please open the supply and demand window in order to load the current supply and demand for export.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }
        else
        {
            if (ImGui.Button("Get schedule for tomorrow"))
            {
                var supplyAndDemand = reader.GetSupplyAndDemand();
                handicrafts = reader.GetHandicrafts();

                scheduleHandler = new ScheduleHandler(handicrafts, supplyAndDemand);
                schedules = scheduleHandler.GetSchedules();
            }

            ImGui.SameLine();

            ImGui.Text("(This might take some time.)");

            if (schedules != null)
            {
                var schedule = schedules.OrderByDescending(x => x.Score).First();

                DrawSchedules(schedule);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
        }
    }

    private void DrawSchedules(Schedule schedule)
    {
        ImGui.Text($"Score: {schedule.Score:0}");

        if (ImGui.BeginTable("Schedule", 4))
        {
            ImGui.TableSetupColumn("#");
            ImGui.TableSetupColumn("Handicraft");
            ImGui.TableSetupColumn("Time");
            ImGui.TableSetupColumn("Categories");
            ImGui.TableHeadersRow();

            for (int i = 0; i < schedule.Handicrafts.Length; i++)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                TextRightAligned($"${i}.");

                ImGui.TableSetColumnIndex(1);
                ImGui.Text(schedule.Handicrafts[i].Name);

                ImGui.TableSetColumnIndex(2);
                TextRightAligned($"{schedule.Handicrafts[i].CraftingTime}h");

                ImGui.TableSetColumnIndex(3);
                ImGui.Text(string.Join(", ", schedule.Handicrafts[i].Categories.Select(x => x.Name)));
            }

            ImGui.EndTable();
        }

        var materials = schedule.Handicrafts
            .SelectMany(x => x.Materials)
            .GroupBy(x => x.InventoryItem)
            .Select(x => new { InventoryItem = x.Key, Amount = x.Sum(y => y.Amount) })
            .OrderBy(x => x.InventoryItem.Id);

        if (ImGui.BeginTable("Materials", 2))
        {
            ImGui.TableSetupColumn("Amount");
            ImGui.TableSetupColumn("Material");
            ImGui.TableHeadersRow();

            foreach (var material in materials)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                TextRightAligned(material.Amount.ToString());

                ImGui.TableSetColumnIndex(1);
                ImGui.Text(material.InventoryItem.Name);
            }

            ImGui.EndTable();
        }
    }

    private static void TextRightAligned(string text)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X - ImGui.GetScrollX());
        ImGui.Text(text);

    }
}