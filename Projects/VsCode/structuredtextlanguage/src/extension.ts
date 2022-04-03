import * as vscode from 'vscode';
import { StructuredTextTaskProvider } from './structuredTextBuildTaskProvider';
import { DebugAdapterDescriptorFactory } from './debug/DebugAdapterDescriptorFactory';

export function activate(_context: vscode.ExtensionContext): void {
	const compilerPath = _context.asAbsolutePath("bin//OfflineCompiler.exe");
	const runtimePath = _context.asAbsolutePath("bin//Runtime.exe");

	const buildTaskProvider = vscode.tasks.registerTaskProvider(
		StructuredTextTaskProvider.Type,
		new StructuredTextTaskProvider(compilerPath));

	const cmdEntryPoint = vscode.commands.registerCommand("extension.structured-text.debug.getEntryPointName",
		config => 
		{
			return vscode.window.showInputBox({
				placeHolder: "Please enter the entry point of the application",
				value: "PLC_PRG"
			})
		});
	const debugAdapterDescriptorFactory = vscode.debug.registerDebugAdapterDescriptorFactory("debug-adapter.structured-text", new DebugAdapterDescriptorFactory(runtimePath));

	_context.subscriptions.push(cmdEntryPoint, buildTaskProvider, debugAdapterDescriptorFactory);
}

export function deactivate(): void {}