// Starter template content and variable definitions for the template editor.
// Kept in a plain JS module (not a Svelte component) so the {{variable}}
// placeholder syntax is never evaluated by Svelte's expression compiler.

export const MJML_STARTER = [
  '<mjml>',
  '  <mj-head>',
  '    <mj-font name="Inter" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap" />',
  '    <mj-attributes>',
  '      <mj-all font-family="Inter, Arial, sans-serif" />',
  '      <mj-text font-size="14px" line-height="1.6" color="#334155" />',
  '    </mj-attributes>',
  '  </mj-head>',
  '  <mj-body background-color="#f1f5f9">',
  '',
  '    <!-- Header -->',
  '    <mj-section background-color="#0f172a" padding="24px 0">',
  '      <mj-column>',
  '        <mj-text align="center" font-size="20px" font-weight="700" color="#ffffff">',
  '          Your Company Name',
  '        </mj-text>',
  '      </mj-column>',
  '    </mj-section>',
  '',
  '    <!-- Body -->',
  '    <mj-section background-color="#ffffff" border-radius="12px" padding="32px 24px">',
  '      <mj-column>',
  '        <mj-text font-size="16px" font-weight="600" color="#0f172a">',
  '          Hello {{applicantName}},',
  '        </mj-text>',
  '        <mj-text>',
  '          Your application reference {{reference}} has been received.',
  '        </mj-text>',
  '        <mj-button background-color="#0ea5e9" border-radius="8px" href="{{siteUrl}}" font-size="14px">',
  '          View Application',
  '        </mj-button>',
  '      </mj-column>',
  '    </mj-section>',
  '',
  '    <!-- Footer -->',
  '    <mj-section padding="16px 0">',
  '      <mj-column>',
  '        <mj-text align="center" font-size="12px" color="#94a3b8">',
  '          &copy; 2026 Your Company. All rights reserved.',
  '        </mj-text>',
  '      </mj-column>',
  '    </mj-section>',
  '',
  '  </mj-body>',
  '</mjml>',
].join('\n');

export const SMS_STARTER =
  'Hi {{applicantName}}, your application {{reference}} has been received. We will be in touch shortly.';

export const DOC_STARTER = [
  '<h1>{{formTitle}}</h1>',
  '<p>Dear {{applicantName}},</p>',
  '<p>Please find attached the details for your application reference <strong>{{reference}}</strong>.</p>',
  '<p>Submitted: {{submittedDate}}</p>',
].join('\n');

export const TEMPLATE_VARIABLES = [
  { key: '{{applicantName}}',  label: 'Applicant name' },
  { key: '{{reference}}',      label: 'Reference number' },
  { key: '{{formTitle}}',      label: 'Form / product name' },
  { key: '{{submittedDate}}',  label: 'Submission date' },
  { key: '{{brokerName}}',     label: 'Broker name' },
  { key: '{{companyName}}',    label: 'Broker company' },
  { key: '{{siteUrl}}',        label: 'Site URL' },
];
