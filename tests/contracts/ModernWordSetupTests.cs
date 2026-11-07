using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KeyJaya
{
    internal static class ModernWordSetupTests
    {
        static ModernWordSetup.ActionInfo Control(string id, string label)
        {
            return new ModernWordSetup.ActionInfo
            {
                Identity = id,
                Label = label,
                Owned = true,
                Visible = true,
                Enabled = true,
                Supported = true
            };
        }

        internal static int Run()
        {
            int passed = 0;
            Action<bool, string> check = (valid, message) =>
            {
                if (!valid)
                    throw new Exception("Modern Word setup: " + message);
                passed++;
            };
            foreach (string failure in new[]
            {
                "none",
                "initial",
                "activation",
                "body",
                "disabled",
                "availability-focus",
                "command-focus",
                "missing-editor",
                "editor-focus",
                "readonly",
                "body-changed"
            }

            )
            {
                bool current = failure != "initial", command = false;
                var order = new List<string>();
                string result = null;
                bool rejected = false;
                try
                {
                    result = ModernWordSetup.Open(() => current, () =>
                    {
                        order.Add("activate");
                        if (failure == "activation")
                            current = false;
                    }, () => failure != "body", () =>
                    {
                        if (failure == "availability-focus")
                            current = false;
                        return failure != "disabled";
                    }, () =>
                    {
                        command = true;
                        order.Add("command");
                        if (failure == "command-focus")
                            current = false;
                    }, () =>
                    {
                        order.Add("find");
                        if (failure == "editor-focus")
                            current = false;
                        return failure == "missing-editor" ? null : "owned editor";
                    }, value => failure != "readonly", () => failure != "body-changed", stage =>
                    {
                    });
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }

                check(failure == "none" ? result == "owned editor" && !rejected : rejected && result == null, "setup outcome: " + failure);
                if (failure == "none")
                    check(string.Join(",", order) == "activate,command,find", "body activation precedes the command and editor discovery");
                if (failure == "initial" || failure == "activation" || failure == "body" || failure == "disabled" || failure == "availability-focus")
                    check(!command && !order.Contains("find"), "failed preconditions never dispatch a comment action: " + failure);
                if (failure == "command-focus")
                    check(!order.Contains("find"), "interruption is rejected before provider discovery");
            }

            check(ModernWordSetup.FindFocused(new[] { "unfocused", "focused", "readonly" }, value => value == "focused", () => true) == "focused", "scoped discovery succeeds without a focus notification");
            check(ModernWordSetup.FindFocused(new[] { "unfocused", "readonly" }, value => value == "focused", () => true) == null, "missing focused editor is never inferred from its position");
            Func<Action, bool> refuses = action =>
            {
                try
                {
                    action();
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            };
            check(refuses(() => ModernWordSetup.FindFocused(new[] { "focused", "focused" }, value => true, () => true)), "ambiguous focused editors are rejected");
            int inspected = 0;
            check(refuses(() => ModernWordSetup.FindFocused(new[] { "focused" }, value =>
            {
                inspected++;
                return true;
            }, () => false)) && inspected == 0, "no provider inspection after initial focus loss");
            bool owns = true;
            check(refuses(() => ModernWordSetup.FindFocused(new[] { "focused" }, value =>
            {
                owns = false;
                return true;
            }, () => owns)), "focus loss during provider work rejects its result");
            int focusChecks = 0;
            check(ModernWordSetup.FindFocused(new[] { "focused" }, value => ++focusChecks == 1, () => true) == null, "editor focus is checked again after enumeration");
            foreach (string setup in new[]
            {
                "SetupOnly",
                "SetupViews",
                "setuponly",
                "setupviews"
            }

            )
                check(!ModernWordSetup.Converts(setup), "setup mode never needs resident settlement: " + setup);
            foreach (string conversion in new[]
            {
                "All",
                "Panel",
                "PanelReply",
                "ContextualNew",
                "ContextualAll",
                "ContextualRecovery",
                "all",
                "panelreply"
            }

            )
                check(ModernWordSetup.Converts(conversion), "conversion mode retains live diagnostic settlement: " + conversion);
            foreach (string unknown in new[]
            {
                "",
                "All ",
                "Unknown",
                null
            }

            )
            {
                bool denied = false;
                try
                {
                    ModernWordSetup.Converts(unknown);
                }
                catch (ArgumentException)
                {
                    denied = true;
                }

                check(denied, "unknown scope is refused before desktop entry");
            }

            foreach (string caption in new[]
            {
                "Comments",
                " &Comments... ",
                "Comments\u2026",
                "Comments (3)",
                "Comments (3)\u2026",
                "Comments (Ctrl+Alt+M)",
                "Comments\tCtrl+Alt+M",
                "\u200eComments\u200f"
            }

            )
                check(ModernWordSetup.Label(caption) == "Comments", "visible caption adornments are normalized: " + caption);
            check(ModernWordSetup.Matches(Control("localized", "Comenta\u0301rios (2)"), new[] { "Comentários" }), "localized Office labels retain Unicode normalization and counts");
            check(!ModernWordSetup.Matches(Control("delete", "Delete Comments"), new[] { "Comments" }), "matching never widens to a destructive label containing Comments");
            check(!ModernWordSetup.Matches(Control("reply", "New comment reply"), new[] { "New comment" }), "matching never widens to an unrelated prefix");
            check(ModernWordSetup.Label(null) == "" && ModernWordSetup.Label(new string ('x', 257)) == "", "missing and excessive captions are unavailable");
            foreach (string failure in new[]
            {
                "owner",
                "hidden",
                "disabled",
                "pattern",
                "identity"
            }

            )
            {
                var candidate = Control("button", "Comments");
                candidate.Owned = failure != "owner";
                candidate.Visible = failure != "hidden";
                candidate.Enabled = failure != "disabled";
                candidate.Supported = failure != "pattern";
                if (failure == "identity")
                    candidate.Identity = "";
                check(!ModernWordSetup.Matches(candidate, new[] { "Comments" }), "unusable action is excluded: " + failure);
            }

            var first = Control("same-runtime", "Comments");
            var alias = Control("same-runtime", "Show Comments");
            var foreign = Control("other-runtime", "Comments");
            foreign.Owned = false;
            var labels = new[]
            {
                "Comments",
                "Show Comments"
            };
            check(ModernWordSetup.FindAction(new[] { foreign, first, alias }, value => value, labels, () => true) == first, "one control discovered through several native roots is not false ambiguity");
            check(refuses(() => ModernWordSetup.FindAction(new[] { first, Control("distinct-runtime", "Comments") }, value => value, labels, () => true)), "two distinct matching controls are ambiguous, never first-match guesses");
            check(ModernWordSetup.FindAction(new[] { foreign }, value => value, labels, () => true) == null, "foreign matching labels never become actions");
            int descriptions = 0;
            check(refuses(() => ModernWordSetup.FindAction(new[] { first }, value =>
            {
                descriptions++;
                return value;
            }, labels, () => false)) && descriptions == 0, "initial focus loss prevents action-provider inspection");
            bool actionCurrent = true;
            check(refuses(() => ModernWordSetup.FindAction(new[] { first }, value =>
            {
                actionCurrent = false;
                return value;
            }, labels, () => actionCurrent)), "focus loss during action discovery rejects all candidates");
            foreach (string late in new[]
            {
                "identity",
                "owner",
                "label",
                "disabled",
                "pattern"
            }

            )
            {
                descriptions = 0;
                check(ModernWordSetup.FindAction(new[] { first }, value =>
                {
                    if (++descriptions == 1)
                        return value;
                    var changed = Control(late == "identity" ? "recycled-runtime" : value.Identity, late == "label" ? "Delete Comments" : value.Label);
                    changed.Owned = late != "owner";
                    changed.Enabled = late != "disabled";
                    changed.Supported = late != "pattern";
                    return changed;
                }, labels, () => true) == null, "final action evidence is revalidated: " + late);
            }

            var excessive = new ModernWordSetup.ActionInfo[2049];
            for (int n = 0; n < excessive.Length; n++)
                excessive[n] = Control("runtime-" + n, "Other");
            descriptions = 0;
            check(refuses(() => ModernWordSetup.FindAction(excessive, value =>
            {
                descriptions++;
                return value;
            }, labels, () => true)) && descriptions == 2048, "provider inspection is bounded even without a matching action");
            int invoked = 0;
            check(refuses(() => ModernWordSetup.Act(() => true, () => false, () => invoked++)) && invoked == 0, "invalidated actions cannot be invoked");
            actionCurrent = true;
            check(refuses(() => ModernWordSetup.Act(() => actionCurrent, () =>
            {
                actionCurrent = false;
                return true;
            }, () => invoked++)) && invoked == 0, "last ownership check precedes invocation");
            ModernWordSetup.Act(() => true, () => true, () => invoked++);
            check(invoked == 1, "one verified action is invoked once");
            int pauses = 0;
            invoked = 0;
            ModernWordSetup.OpenView(() => true, () => true, () => invoked++, () => pauses++);
            check(invoked == 0 && pauses == 0, "an already visible panel is never toggled closed");
            int viewSamples = 0;
            ModernWordSetup.OpenView(() => true, () => ++viewSamples >= 3, () => invoked++, () => pauses++);
            check(invoked == 1 && viewSamples >= 4, "opening waits for a stable actual panel without repeated toggles");
            invoked = pauses = 0;
            check(refuses(() => ModernWordSetup.OpenView(() => true, () => false, () => invoked++, () => pauses++)) && invoked == 1 && pauses == 59, "a missing view times out after one invocation");
            actionCurrent = true;
            invoked = 0;
            check(refuses(() => ModernWordSetup.OpenView(() => actionCurrent, () =>
            {
                actionCurrent = false;
                return false;
            }, () => invoked++, () =>
            {
            })) && invoked == 0, "focus lost during view discovery prevents invocation");
            int samples = 0;
            bool[] readiness =
            {
                true,
                false,
                true,
                true,
                true
            };
            check(ModernWordSetup.Wait(() => true, () => readiness[samples++], () =>
            {
            }, 5, 3) && samples == 5, "transient readiness resets the consecutive sample requirement");
            samples = pauses = 0;
            check(!ModernWordSetup.Wait(() => true, () =>
            {
                samples++;
                return false;
            }, () => pauses++, 4, 2) && samples == 4 && pauses == 3, "unavailable readiness is bounded without an extra final pause");
            samples = 0;
            check(refuses(() => ModernWordSetup.Wait(() => false, () =>
            {
                samples++;
                return true;
            }, () =>
            {
            }, 4, 2)) && samples == 0, "no readiness provider is queried after focus loss");
            foreach (bool recovery in new[]
            {
                false,
                true
            }

            )
            {
                var order = new List<string>();
                int readyCalls = 0;
                ModernWordSetup.Seed("prepared", recovery, () => true, value => order.Add("replace:" + value), value => order.Add("type:" + value), value => order.Add("verify:" + value), () => order.Add("caret"), () =>
                {
                    readyCalls++;
                    return true;
                }, () =>
                {
                });
                check(string.Join(",", order) == (recovery ? "replace:prepared,verify:prepared,caret" : "replace:,type:prepared,verify:prepared,caret"), "native input is used for normal setup only; recovery keeps lost notifications");
                check(readyCalls == (recovery ? 0 : 3), "recovery never warms focus metadata; normal setup requires stable evidence");
            }

            foreach (string failure in new[]
            {
                "initial",
                "replace",
                "type",
                "verify",
                "caret",
                "readiness"
            }

            )
            {
                bool current = failure != "initial";
                var order = new List<string>();
                bool denied = refuses(() => ModernWordSetup.Seed("prepared", false, () => current, value =>
                {
                    order.Add("replace");
                    if (failure == "replace")
                        current = false;
                }, value =>
                {
                    order.Add("type");
                    if (failure == "type")
                        current = false;
                }, value =>
                {
                    order.Add("verify");
                    if (failure == "verify")
                        throw new InvalidOperationException();
                }, () =>
                {
                    order.Add("caret");
                    if (failure == "caret")
                        current = false;
                }, () => failure != "readiness", () =>
                {
                }));
                check(denied, "seeding refuses interrupted or unsettled input: " + failure);
                if (failure == "initial" || failure == "replace")
                    check(!order.Contains("type"), "no native typing after earlier ownership loss");
                if (failure == "type" || failure == "verify")
                    check(!order.Contains("caret"), "no caret movement after failed typing/text proof");
                if (failure == "readiness")
                    check(order.FindAll(value => value == "type").Count == 1, "unsettled focus never retypes input or retries conversion");
            }

            check(ModernWordSetup.PostedCard(new[] { "draftCardRoot_4_temporary-guid" }) == null, "temporary draft IDs are not fabricated into posted server IDs");
            check(ModernWordSetup.PostedCard(new[] { "draftCardRoot_reply", "cardRoot_4_server", "" }) == "cardRoot_4_server", "reply retains its actual posted parent thread");
            check(ModernWordSetup.PostedCard(new[] { "cardRoot_inner", "cardRoot_outer" }) == "cardRoot_outer", "outer owning thread is retained for nested reply cards");
            var before = new HashSet<string>(new[] { "cardRoot_old", "cardRoot_other", "cardRoot_reply" }, StringComparer.Ordinal);
            check(ModernWordSetup.PostedMatch("cardRoot_new-server-id", null, before), "a new parent is found by before/after evidence despite server ID changes");
            check(!ModernWordSetup.PostedMatch("cardRoot_old", null, before) && !ModernWordSetup.PostedMatch("draftCardRoot_temp", null, before), "old threads and unposted drafts cannot become a new posted target");
            check(ModernWordSetup.PostedMatch("cardRoot_old", "cardRoot_old", before) && !ModernWordSetup.PostedMatch("cardRoot_other", "cardRoot_old", before), "editing/reply targets the exact existing thread");
            check(refuses(() => ModernWordSetup.FindFocused(new[] { "cardRoot_new1", "cardRoot_new2" }, value => ModernWordSetup.PostedMatch(value, null, before), () => true)), "multiple new posted candidates remain ambiguous");
            int queried = 0;
            const string contextualCard = "cardRoot_4_947445465";
            check(ModernWordSetup.SamePostedThread("cardRoot_1_947445465", contextualCard), "the actual panel and contextual IDs retain the same posted thread across views");
            check(ModernWordSetup.SamePostedThread(contextualCard, contextualCard), "same-view posted thread still matches");
            check(!ModernWordSetup.SamePostedThread("cardRoot_1_947445466", contextualCard), "different posted thread cannot anchor the panel");
            check(!ModernWordSetup.SamePostedThread("draftCardRoot_1_947445465", contextualCard) && !ModernWordSetup.SamePostedThread("postRoot_1_947445465", contextualCard), "drafts and reply posts cannot masquerade as owning threads");
            check(!ModernWordSetup.SamePostedThread("cardRoot_1_947445465_extra", contextualCard) && !ModernWordSetup.SamePostedThread("cardRoot__947445465", contextualCard), "unknown card ID formats refuse view rebinding");
            check(!ModernWordSetup.SamePostedThread(null, contextualCard) && !ModernWordSetup.SamePostedThread(contextualCard, null) && !ModernWordSetup.SamePostedThread("CardRoot_1_947445465", contextualCard), "missing and wrong-case card IDs refuse rebinding");
            check(!ModernWordSetup.SamePostedThread("cardRoot_1_9474454650", contextualCard) && !ModernWordSetup.SamePostedThread("cardRoot_1_0947445465", contextualCard), "thread IDs match completely without numeric normalization");
            check(!ModernWordSetup.SamePostedThread("cardRoot_1_" + new string ('9', 129), contextualCard), "oversized card IDs refuse view rebinding");
            var anchored = new List<string>(ModernWordSetup.Anchored(new[] { "ribbon", "comment-host" }, value => value == "comment-host", value =>
            {
                check(value == "comment-host", "unanchored ribbon controls are never queried");
                queried++;
                return new[]
                {
                    "New"
                };
            }, () => true));
            check(queried == 1 && anchored.Count == 1 && anchored[0] == "New", "only the anchored comment region supplies panel controls");
            bool anchorCurrent = true;
            queried = 0;
            check(refuses(() => new List<string>(ModernWordSetup.Anchored(new[] { "comment-host" }, value =>
            {
                anchorCurrent = false;
                return true;
            }, value =>
            {
                queried++;
                return new[]
                {
                    "New"
                };
            }, () => anchorCurrent))) && queried == 0, "focus lost while proving the anchor prevents control enumeration");
            int anchorReads = 0;
            var regions = new string[65];
            check(refuses(() => new List<string>(ModernWordSetup.Anchored(regions, value =>
            {
                anchorReads++;
                return false;
            }, value => new[] { "New" }, () => true))) && anchorReads == 64, "native region discovery is bounded without a matching anchor");
            queried = 0;
            check(refuses(() => new List<string>(ModernWordSetup.Anchored(new[] { "comment-host" }, value =>
            {
                queried++;
                return true;
            }, value => new[] { "New" }, () => false))) && queried == 0, "initial focus loss prevents even anchor inspection");
            const string recovered = "Word focus helper ready process=72\ndiagnostic Word focus locator resolved=True\n";
            check(ModernWordSetup.RecoveredLocator(recovered, 72), "replacement helper requires actual resolved locator evidence");
            check(!ModernWordSetup.RecoveredLocator(recovered, 7) && !ModernWordSetup.RecoveredLocator(recovered, 0), "helper identities match complete process numbers");
            check(!ModernWordSetup.RecoveredLocator("Word focus helper ready process=72\n", 72), "ready alone does not prove comment catch-up completion");
            check(!ModernWordSetup.RecoveredLocator(recovered + "Word focus helper ready process=81\n", 72), "a retired helper's successful lookup is stale");
            check(!ModernWordSetup.RecoveredLocator(recovered + "Word focus unavailable; refreshing only the focus helper; resident remains running.\n", 72), "refresh invalidates earlier locator readiness");
            check(!ModernWordSetup.RecoveredLocator(recovered + "diagnostic Word focus locator resolved=False\n", 72), "a later failed lookup overrides earlier success");
            check(!ModernWordSetup.RecoveredLocator(recovered + "Word focus helper ready process=72\n", 72), "a new readiness epoch cannot reuse earlier resolution");
            foreach (string failure in new[]
            {
                "Word focus helper exited; retry in 250 ms",
                "Word focus helper not ready; retry in 250 ms",
                "Word focus helper unavailable: IOException"
            }

            )
                check(!ModernWordSetup.RecoveredLocator(recovered + failure + "\n", 72), "helper failure retires locator readiness: " + failure);
            foreach (string setup in new[]
            {
                "SetupOnly",
                "SetupViews"
            }

            )
            {
                int prepares = 0, posts = 0;
                check(refuses(() => ModernWordSetup.Dispatch(setup, () => true, () => prepares++, () => posts++)) && prepares == 0 && posts == 0, "setup dispatch is rejected before preparation or native messages: " + setup);
            }

            int prepared = 0, posted = 0;
            ModernWordSetup.Dispatch("All", () => true, () => prepared++, () => posted++);
            check(prepared == 1 && posted == 1, "conversion dispatch occurs once after preparation");
            bool dispatchCurrent = true;
            posted = 0;
            check(refuses(() => ModernWordSetup.Dispatch("All", () => dispatchCurrent, () => dispatchCurrent = false, () => posted++)) && posted == 0, "focus lost during preparation prevents the actual conversion");
            check(refuses(() => ModernWordSetup.Dispatch("All", () => true, () =>
            {
                throw new InvalidOperationException("Unverified/expired settlement");
            }, () => posted++)) && posted == 0, "failed settlement never becomes permission to dispatch");
            int boundaries = 0, boundaryReads = 0, boundaryPauses = 0;
            ModernWordSetup.SetupUndoBoundary(false, () => true, () => boundaries++, () => ++boundaryReads >= 2, () => boundaryPauses++);
            check(boundaries == 1 && boundaryReads == 3 && boundaryPauses == 2, "one setup boundary waits for two stable unchanged observations");
            boundaries = boundaryReads = 0;
            ModernWordSetup.SetupUndoBoundary(true, () =>
            {
                boundaryReads++;
                return true;
            }, () => boundaries++, () =>
            {
                boundaryReads++;
                return true;
            }, () => boundaryReads++);
            check(boundaries == 0 && boundaryReads == 0, "recovery performs no new boundary input or readiness observations");
            check(refuses(() => ModernWordSetup.SetupUndoBoundary(false, () => false, () => boundaries++, () => true, () =>
            {
            })) && boundaries == 0, "foreign initial focus prevents setup navigation");
            bool boundaryCurrent = true;
            check(refuses(() => ModernWordSetup.SetupUndoBoundary(false, () => boundaryCurrent, () =>
            {
                boundaries++;
                boundaryCurrent = false;
            }, () =>
            {
                boundaryReads++;
                return true;
            }, () =>
            {
            })) && boundaries == 1 && boundaryReads == 0, "ownership loss after navigation prevents any readiness proof");
            boundaries = boundaryReads = 0;
            check(refuses(() => ModernWordSetup.SetupUndoBoundary(false, () => true, () => boundaries++, () =>
            {
                boundaryReads++;
                return false;
            }, () =>
            {
            })) && boundaries == 1 && boundaryReads == 20, "changed text/caret is bounded and never causes repeated setup input");
            boundaryCurrent = true;
            check(refuses(() => ModernWordSetup.SetupUndoBoundary(false, () => boundaryCurrent, () =>
            {
            }, () =>
            {
                boundaryCurrent = false;
                return true;
            }, () =>
            {
            })), "focus change during verification cannot authorize conversion");
            boundaries = 0;
            check(refuses(() => ModernWordSetup.SetupUndoBoundary(false, () => true, () =>
            {
                boundaries++;
                throw new InvalidOperationException();
            }, () => true, () =>
            {
            })) && boundaries == 1, "a failed navigation is not retried");
            check(ModernWordSetup.CompleteLog(null) == "" && ModernWordSetup.CompleteLog("partial") == "", "incomplete log records are not observations");
            const string logReady = "timestamp Word focus helper ready process=72\n";
            check(ModernWordSetup.CompleteLog(logReady + "partial") == logReady, "a snapshot excludes its unfinished last record");
            check(ModernWordSetup.LoggedHelper(logReady + "Word focus helper ready process=8") == 72, "a partial new PID cannot replace the last complete helper identity");
            check(ModernWordSetup.LoggedHelper("Word focus helper ready process=72") == 0, "unterminated readiness alone cannot authorize a helper");
            check(ModernWordSetup.LoggedHelper(logReady + "Word focus helper ready process=81\r\n") == 81, "latest complete CRLF readiness selects the exact helper");
            check(ModernWordSetup.LoggedHelper("Word focus helper ready process=0\n") == 0 && ModernWordSetup.LoggedHelper("Word focus helper ready process=999999999999999999\n") == 0, "zero and overflowing helper identities are refused");
            string logDirectory = Path.Combine(Environment.CurrentDirectory, "out", "shared-log-contract-" + Guid.NewGuid().ToString("N"));
            string logPath = Path.Combine(logDirectory, "fixture.log");
            Directory.CreateDirectory(logDirectory);
            try
            {
                using (var stream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.AutoFlush = true;
                    writer.Write(logReady);
                    check(ModernWordSetup.ReadLog(logPath) == logReady, "recovery reads while the resident's append writer remains open");
                    writer.Write("Word focus helper ready process=8");
                    check(ModernWordSetup.LoggedHelper(ModernWordSetup.ReadLog(logPath)) == 72, "an in-flight append does not publish a partial helper PID");
                    writer.Write("1\n");
                    check(ModernWordSetup.LoggedHelper(ModernWordSetup.ReadLog(logPath)) == 81, "the completed append is visible without reopening the writer");
                    writer.Write(new string ('x', 1024 * 1024));
                    check(refuses(() => ModernWordSetup.ReadLog(logPath)), "an oversized fixture log is bounded without editing it");
                }
            }
            finally
            {
                File.Delete(logPath);
                Directory.Delete(logDirectory);
            }

            var counts = new Dictionary<string, int>
            {
                {
                    "SetupOnly",
                    0
                },
                {
                    "SetupViews",
                    0
                },
                {
                    "All",
                    33
                },
                {
                    "ContextualNew",
                    7
                },
                {
                    "ContextualAll",
                    17
                },
                {
                    "Panel",
                    16
                },
                {
                    "PanelReply",
                    6
                },
                {
                    "ContextualRecovery",
                    1
                }
            };
            foreach (var entry in counts)
            {
                string[] keys = ModernWordSetup.CaseKeys(entry.Key);
                check(keys.Length == entry.Value, "each requested scope declares its actual conversion coverage: " + entry.Key);
                check(ModernWordSetup.Complete(entry.Key, keys), "complete coverage is accepted: " + entry.Key);
                if (keys.Length != 0)
                {
                    Array.Reverse(keys);
                    check(ModernWordSetup.Complete(entry.Key, keys), "coverage does not depend on action order: " + entry.Key);
                }
            }

            var full = new List<string>(ModernWordSetup.CaseKeys("All"));
            foreach (string key in full)
            {
                var missing = new List<string>(full);
                missing.Remove(key);
                check(!ModernWordSetup.Complete("All", missing), "a missing view/action refuses a full pass: " + key);
                var duplicate = new List<string>(full);
                duplicate.Add(key);
                check(!ModernWordSetup.Complete("All", duplicate), "a repeated view/action refuses a full pass: " + key);
            }

            check(full.Contains("contextual posted edit:word") && full.Contains("panel posted edit:word"), "the two existing-comment views have separate coverage evidence");
            check(!ModernWordSetup.Complete("All", new string[0]), "an empty run cannot become a full pass");
            check(!ModernWordSetup.Complete("SetupViews", new[] { "contextual new:word" }), "a setup scope cannot claim any conversion coverage");
            var wrong = new List<string>(full);
            wrong[0] = "unknown view:word";
            check(!ModernWordSetup.Complete("All", wrong), "unknown view/action cannot replace a missing case");
            wrong[0] = "Contextual new:word";
            check(!ModernWordSetup.Complete("All", wrong), "coverage identity is exact and case sensitive");
            Console.WriteLine("Modern Word setup: " + passed + " assertions; owned focus, native seeding, stable readiness, fresh actions, thread targeting and scope/locator guards; no Word/UIA/input.");
            return passed;
        }
    }
}
