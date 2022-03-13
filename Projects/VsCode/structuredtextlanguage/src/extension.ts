import * as vscode from 'vscode';
import { StructuredTextTaskProvider } from './structuredTextBuildTaskProvider';

let buildTaskProvider: vscode.Disposable | undefined;

export function activate(_context: vscode.ExtensionContext): void {
	const workspaceRoot = (vscode.workspace.workspaceFolders && (vscode.workspace.workspaceFolders.length > 0))
		? vscode.workspace.workspaceFolders[0].uri.fsPath : undefined;
	if (!workspaceRoot) {
		return;
	}
	const compilerPath = _context.asAbsolutePath("bin//OfflineCompiler.exe");
		
	buildTaskProvider = vscode.tasks.registerTaskProvider(
		StructuredTextTaskProvider.Type,
		new StructuredTextTaskProvider(compilerPath));
}

export function deactivate(): void {
	if (buildTaskProvider) {
		buildTaskProvider.dispose();
	}
}