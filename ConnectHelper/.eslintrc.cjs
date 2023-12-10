module.exports = {
  root: true,
  env: { browser: true, es2020: true },
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react-hooks/recommended',
		'plugin:react/recommended',
		'prettier',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs'],
  parser: '@typescript-eslint/parser',
  plugins: ['react-refresh'],
  rules: {
    'react-refresh/only-export-components': [
      'warn',
      { allowConstantExport: true },
    ],
		"react/jsx-uses-react": "off",
    "react/react-in-jsx-scope": "off",
  },
	settings: {
    react: {
      "createClass": "createReactClass",
      "pragma": "React",
      "fragment": "Fragment",
      "version": "detect",
      "flowVersion": "0.53"
    },
    "propWrapperFunctions": [
        "forbidExtraProps",
        {"property": "freeze", "object": "Object"},
        {"property": "myFavoriteWrapper"},
        {"property": "forbidExtraProps", "exact": true}
    ],
    "componentWrapperFunctions": [
        "observer",
        {"property": "styled"},
        {"property": "observer", "object": "Mobx"},
        {"property": "observer", "object": "<pragma>"}
    ],
    "formComponents": [
      "CustomForm",
      {"name": "Form", "formAttribute": "endpoint"}
    ],
    "linkComponents": [
      "Hyperlink",
      {"name": "Link", "linkAttribute": "to"}
    ]
  },
}
